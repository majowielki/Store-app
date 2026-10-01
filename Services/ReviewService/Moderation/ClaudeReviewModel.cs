using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;

namespace Store.ReviewService.Moderation;

/// <summary>
/// Claude as the first reader of new reviews (ADR 019): one short request per review, the answer
/// constrained to a verdict and a reason. A model that cannot be reached gives no verdict and the
/// review waits for the administrator; a refusal or an answer cut short is a "doubtful". The
/// review's text is never logged.
/// </summary>
public sealed class ClaudeReviewModel : IReviewModel
{
    private readonly IAnthropicClient _client;
    private readonly ReviewModelOptions _options;
    private readonly ILogger<ClaudeReviewModel> _logger;

    public ClaudeReviewModel(IAnthropicClient client, IOptions<ReviewModelOptions> options, ILogger<ClaudeReviewModel> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ModelJudgement?> JudgeAsync(ReviewForModel review, CancellationToken cancellationToken = default)
    {
        Message response;
        try
        {
            response = await _client.Messages.Create(new MessageCreateParams
            {
                Model = _options.Model,
                MaxTokens = _options.MaxTokens,
                System = ReviewModelPrompt.Instructions,
                Messages = [new() { Role = Role.User, Content = ReviewModelPrompt.Review(review) }],
                OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = ReviewModelPrompt.AnswerSchema } }
            }, cancellationToken);
        }
        catch (AnthropicApiException ex)
        {
            // Refused (a bad key, a malformed request) or overloaded past the client's own retries
            _logger.LogWarning("The review model answered with an error ({ErrorType}); the review waits for the administrator", ex.GetType().Name);
            return null;
        }
        catch (AnthropicIOException ex)
        {
            _logger.LogWarning("The review model could not be reached ({ErrorType}); the review waits for the administrator", ex.GetType().Name);
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("The review model did not answer in {Seconds} s; the review waits for the administrator", _options.TimeoutSeconds);
            return null;
        }

        if (response.StopReason != StopReason.EndTurn)
        {
            _logger.LogInformation("The review model stopped with {StopReason}; the review waits for the administrator", response.StopReason);
            return ReviewModelPrompt.NoVerdict;
        }

        var answer = string.Concat(response.Content.Select(block => block.Value).OfType<TextBlock>().Select(text => text.Text));
        return ReviewModelPrompt.Read(answer);
    }
}
