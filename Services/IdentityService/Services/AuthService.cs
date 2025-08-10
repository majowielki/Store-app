using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Store.IdentityService.DTOs.Requests;
using Store.IdentityService.DTOs.Responses;
using Store.IdentityService.Models;
using Store.Shared.Models;
using Store.Shared.Utility;
using Store.Shared.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Store.IdentityService.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Store.Shared.Services.IAuditLogClient _auditLogClient;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IHttpContextAccessor httpContextAccessor,
        Store.Shared.Services.IAuditLogClient auditLogClient)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _auditLogClient = auditLogClient;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();
        
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                // Log failed registration attempt - user already exists
                await LogAuthenticationEventAsync(new AuditLog
                {
                    Action = "USER_REGISTRATION_FAILED",
                    EntityName = "ApplicationUser",
                    UserEmail = request.Email,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AdditionalInfo = JsonSerializer.Serialize(new
                    {
                        Reason = "EmailAlreadyExists",
                        AttemptedEmail = request.Email,
                        AttemptedFirstName = request.FirstName,
                        AttemptedLastName = request.LastName
                    })
                });

                return new AuthResponse
                {
                    Success = false,
                    Message = "User with this email already exists"
                };
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                
                // Log failed registration attempt - validation errors
                await LogAuthenticationEventAsync(new AuditLog
                {
                    Action = "USER_REGISTRATION_FAILED",
                    EntityName = "ApplicationUser",
                    UserEmail = request.Email,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AdditionalInfo = JsonSerializer.Serialize(new
                    {
                        Reason = "ValidationErrors",
                        Errors = errors,
                        AttemptedEmail = request.Email
                    })
                });

                return new AuthResponse
                {
                    Success = false,
                    Message = string.Join(", ", errors)
                };
            }

            await EnsureRoleExistsAsync(Constants.Role_User);
            await _userManager.AddToRoleAsync(user, Constants.Role_User);

            // Log successful registration
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "USER_REGISTRATION_SUCCESS",
                EntityName = "ApplicationUser",
                EntityId = user.Id,
                UserId = user.Id,
                UserEmail = user.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                NewValues = JsonSerializer.Serialize(new
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = new[] { Constants.Role_User }
                }),
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    AccountStatus = "Active",
                    EmailConfirmed = true,
                    AutoAssignedRole = Constants.Role_User
                })
            });

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(user);

            // Log token generation
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "JWT_TOKEN_GENERATED",
                EntityName = "JWT",
                UserId = user.Id,
                UserEmail = user.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    TokenType = "Registration",
                    ExpiresAt = expiresAt,
                    UserRoles = new[] { Constants.Role_User }
                })
            });

            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(user)
            };
        }
        catch (Exception ex)
        {
            // Log system error during registration
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "USER_REGISTRATION_ERROR",
                EntityName = "ApplicationUser",
                UserEmail = request.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    ErrorMessage = ex.Message,
                    ErrorType = ex.GetType().Name,
                    AttemptedEmail = request.Email
                })
            });

            _logger.LogError(ex, "Error during user registration");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during registration"
            };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();
        
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // Log failed login attempt - user not found
                await LogAuthenticationEventAsync(new AuditLog
                {
                    Action = "USER_LOGIN_FAILED",
                    EntityName = "ApplicationUser",
                    UserEmail = request.Email,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AdditionalInfo = JsonSerializer.Serialize(new
                    {
                        Reason = "UserNotFound",
                        AttemptedEmail = request.Email
                    })
                });

                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            if (!user.IsActive)
            {
                // Log failed login attempt - account deactivated
                await LogAuthenticationEventAsync(new AuditLog
                {
                    Action = "USER_LOGIN_FAILED",
                    EntityName = "ApplicationUser",
                    EntityId = user.Id,
                    UserId = user.Id,
                    UserEmail = user.Email,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AdditionalInfo = JsonSerializer.Serialize(new
                    {
                        Reason = "AccountDeactivated",
                        AttemptedEmail = request.Email
                    })
                });

                return new AuthResponse
                {
                    Success = false,
                    Message = "Account is deactivated"
                };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                // Log failed login attempt - invalid password
                await LogAuthenticationEventAsync(new AuditLog
                {
                    Action = "USER_LOGIN_FAILED",
                    EntityName = "ApplicationUser",
                    EntityId = user.Id,
                    UserId = user.Id,
                    UserEmail = user.Email,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AdditionalInfo = JsonSerializer.Serialize(new
                    {
                        Reason = "InvalidPassword",
                        AttemptedEmail = request.Email,
                        IsLockedOut = result.IsLockedOut,
                        IsNotAllowed = result.IsNotAllowed,
                        RequiresTwoFactor = result.RequiresTwoFactor
                    })
                });

                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }

            // Update last login time
            var oldLastLoginAt = user.LastLoginAt;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(user);

            // Log successful login
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "USER_LOGIN_SUCCESS",
                EntityName = "ApplicationUser",
                EntityId = user.Id,
                UserId = user.Id,
                UserEmail = user.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                OldValues = JsonSerializer.Serialize(new
                {
                    LastLoginAt = oldLastLoginAt
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    LastLoginAt = user.LastLoginAt
                }),
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    LoginMethod = "EmailPassword",
                    TokenExpiresAt = expiresAt,
                    UserRoles = await _userManager.GetRolesAsync(user)
                })
            });

            // Log token generation
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "JWT_TOKEN_GENERATED",
                EntityName = "JWT",
                UserId = user.Id,
                UserEmail = user.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    TokenType = "Login",
                    ExpiresAt = expiresAt,
                    UserRoles = await _userManager.GetRolesAsync(user)
                })
            });

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(user)
            };
        }
        catch (Exception ex)
        {
            // Log system error during login
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "USER_LOGIN_ERROR",
                EntityName = "ApplicationUser",
                UserEmail = request.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    ErrorMessage = ex.Message,
                    ErrorType = ex.GetType().Name,
                    AttemptedEmail = request.Email
                })
            });

            _logger.LogError(ex, "Error during login");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during login"
            };
        }
    }

    public async Task<AuthResponse> DemoLoginAsync(DemoLoginRequest request)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();
        
        try
        {
            var demoUser = await _userManager.FindByEmailAsync(Constants.DemoUserEmail);
            if (demoUser == null)
            {
                // Log demo user not found error
                await LogAuthenticationEventAsync(new AuditLog
                {
                    Action = "DEMO_LOGIN_FAILED",
                    EntityName = "ApplicationUser",
                    UserEmail = Constants.DemoUserEmail,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AdditionalInfo = JsonSerializer.Serialize(new
                    {
                        Reason = "DemoUserNotFound",
                        ExpectedEmail = Constants.DemoUserEmail
                    })
                });

                _logger.LogError("Demo user not found. Should be created during database initialization.");
                return new AuthResponse
                {
                    Success = false,
                    Message = "Demo user not available"
                };
            }

            // Update last login time
            var oldLastLoginAt = demoUser.LastLoginAt;
            demoUser.LastLoginAt = DateTime.UtcNow;
            demoUser.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(demoUser);

            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(demoUser);

            // Log successful demo login
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "DEMO_LOGIN_SUCCESS",
                EntityName = "ApplicationUser",
                EntityId = demoUser.Id,
                UserId = demoUser.Id,
                UserEmail = demoUser.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                OldValues = JsonSerializer.Serialize(new
                {
                    LastLoginAt = oldLastLoginAt
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    LastLoginAt = demoUser.LastLoginAt
                }),
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    LoginMethod = "DemoLogin",
                    TokenExpiresAt = expiresAt,
                    UserRoles = await _userManager.GetRolesAsync(demoUser)
                })
            });

            // Log demo token generation
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "JWT_TOKEN_GENERATED",
                EntityName = "JWT",
                UserId = demoUser.Id,
                UserEmail = demoUser.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    TokenType = "DemoLogin",
                    ExpiresAt = expiresAt,
                    UserRoles = await _userManager.GetRolesAsync(demoUser)
                })
            });

            _logger.LogInformation("Demo user logged in successfully");

            return new AuthResponse
            {
                Success = true,
                Message = "Demo login successful",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(demoUser)
            };
        }
        catch (Exception ex)
        {
            // Log demo login error
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "DEMO_LOGIN_ERROR",
                EntityName = "ApplicationUser",
                UserEmail = Constants.DemoUserEmail,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    ErrorMessage = ex.Message,
                    ErrorType = ex.GetType().Name
                })
            });

            _logger.LogError(ex, "Error during demo login");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during demo login"
            };
        }
    }

    public async Task<AuthResponse> DemoAdminLoginAsync(DemoAdminLoginRequest request)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();
        
        try
        {
            var demoAdmin = await _userManager.FindByEmailAsync(Constants.DemoAdminEmail);
            if (demoAdmin == null)
            {
                // Log demo admin not found error
                await LogAuthenticationEventAsync(new AuditLog
                {
                    Action = "DEMO_ADMIN_LOGIN_FAILED",
                    EntityName = "ApplicationUser",
                    UserEmail = Constants.DemoAdminEmail,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AdditionalInfo = JsonSerializer.Serialize(new
                    {
                        Reason = "DemoAdminNotFound",
                        ExpectedEmail = Constants.DemoAdminEmail
                    })
                });

                _logger.LogError("Demo admin not found. Should be created during database initialization.");
                return new AuthResponse
                {
                    Success = false,
                    Message = "Demo admin not available"
                };
            }

            // Update last login time
            var oldLastLoginAt = demoAdmin.LastLoginAt;
            demoAdmin.LastLoginAt = DateTime.UtcNow;
            demoAdmin.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(demoAdmin);

            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(demoAdmin);

            // Log successful demo admin login
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "DEMO_ADMIN_LOGIN_SUCCESS",
                EntityName = "ApplicationUser",
                EntityId = demoAdmin.Id,
                UserId = demoAdmin.Id,
                UserEmail = demoAdmin.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                OldValues = JsonSerializer.Serialize(new
                {
                    LastLoginAt = oldLastLoginAt
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    LastLoginAt = demoAdmin.LastLoginAt
                }),
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    LoginMethod = "DemoAdminLogin",
                    TokenExpiresAt = expiresAt,
                    UserRoles = await _userManager.GetRolesAsync(demoAdmin)
                })
            });

            // Log demo admin token generation
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "JWT_TOKEN_GENERATED",
                EntityName = "JWT",
                UserId = demoAdmin.Id,
                UserEmail = demoAdmin.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    TokenType = "DemoAdminLogin",
                    ExpiresAt = expiresAt,
                    UserRoles = await _userManager.GetRolesAsync(demoAdmin)
                })
            });

            _logger.LogInformation("Demo admin logged in successfully");

            return new AuthResponse
            {
                Success = true,
                Message = "Demo admin login successful",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(demoAdmin)
            };
        }
        catch (Exception ex)
        {
            // Log demo admin login error
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "DEMO_ADMIN_LOGIN_ERROR",
                EntityName = "ApplicationUser",
                UserEmail = Constants.DemoAdminEmail,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    ErrorMessage = ex.Message,
                    ErrorType = ex.GetType().Name
                })
            });

            _logger.LogError(ex, "Error during demo admin login");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during demo admin login"
            };
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!);
            
            ClaimsPrincipal principal;
            try
            {
                principal = tokenHandler.ValidateToken(request.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);
            }
            catch (Exception tokenEx)
            {
                // Log invalid token refresh attempt
                await LogAuthenticationEventAsync(new AuditLog
                {
                    Action = "TOKEN_REFRESH_FAILED",
                    EntityName = "JWT",
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AdditionalInfo = JsonSerializer.Serialize(new
                    {
                        Reason = "InvalidToken",
                        TokenError = tokenEx.Message,
                        ProvidedToken = request.Token?.Substring(0, Math.Min(50, request.Token.Length)) + "..."
                    })
                });

                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                // Log token refresh failure - no user ID in token
                await LogAuthenticationEventAsync(new AuditLog
                {
                    Action = "TOKEN_REFRESH_FAILED",
                    EntityName = "JWT",
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AdditionalInfo = JsonSerializer.Serialize(new
                    {
                        Reason = "NoUserIdInToken"
                    })
                });

                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                // Log token refresh failure - user not found or inactive
                await LogAuthenticationEventAsync(new AuditLog
                {
                    Action = "TOKEN_REFRESH_FAILED",
                    EntityName = "JWT",
                    UserId = userId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AdditionalInfo = JsonSerializer.Serialize(new
                    {
                        Reason = user == null ? "UserNotFound" : "UserInactive",
                        UserId = userId
                    })
                });

                return new AuthResponse
                {
                    Success = false,
                    Message = "User not found or inactive"
                };
            }

            var (accessToken, expiresAt) = await GenerateAccessTokenAsync(user);

            // Log successful token refresh
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "TOKEN_REFRESH_SUCCESS",
                EntityName = "JWT",
                UserId = user.Id,
                UserEmail = user.Email,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    NewTokenExpiresAt = expiresAt,
                    UserRoles = await _userManager.GetRolesAsync(user)
                })
            });

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);

            return new AuthResponse
            {
                Success = true,
                Message = "Token refreshed successfully",
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                User = await MapToUserResponseAsync(user)
            };
        }
        catch (Exception ex)
        {
            // Log token refresh system error
            await LogAuthenticationEventAsync(new AuditLog
            {
                Action = "TOKEN_REFRESH_ERROR",
                EntityName = "JWT",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalInfo = JsonSerializer.Serialize(new
                {
                    ErrorMessage = ex.Message,
                    ErrorType = ex.GetType().Name
                })
            });

            _logger.LogError(ex, "Error during token refresh");
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during token refresh"
            };
        }
    }

    public async Task<ApiResponse<UserResponse>> GetCurrentUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserResponse>.Error("User not found");
            }

            var userResponse = await MapToUserResponseAsync(user);
            return ApiResponse<UserResponse>.Success(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return ApiResponse<UserResponse>.Error("An error occurred while retrieving user information");
        }
    }

    public async Task<ApiResponse<UserResponse>> GetUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserResponse>.Error("User not found");
            }

            var userResponse = await MapToUserResponseAsync(user);
            return ApiResponse<UserResponse>.Success(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user");
            return ApiResponse<UserResponse>.Error("An error occurred while retrieving user information");
        }
    }

    public async Task<ApiResponse<IEnumerable<UserResponse>>> GetAllUsersAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var users = await _userManager.Users
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var userResponses = new List<UserResponse>();
            foreach (var user in users)
            {
                userResponses.Add(await MapToUserResponseAsync(user));
            }

            return ApiResponse<IEnumerable<UserResponse>>.Success(userResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return ApiResponse<IEnumerable<UserResponse>>.Error("An error occurred while retrieving users");
        }
    }

    private async Task<(string token, DateTime expiresAt)> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!);
        var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpirationInMinutes"]!));

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new("firstName", user.FirstName ?? ""),
            new("lastName", user.LastName ?? ""),
            new("displayName", user.DisplayName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiresAt);
    }

    private async Task<UserResponse> MapToUserResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            SimpleAddress = user.SimpleAddress,
            Roles = roles.ToList(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    /// <summary>
    /// Helper method to log authentication events to the audit service
    /// </summary>
    private async Task LogAuthenticationEventAsync(AuditLog auditLog)
    {
        try
        {
            // Ensure timestamp is set
            if (auditLog.Timestamp == default)
            {
                auditLog.Timestamp = DateTime.UtcNow;
            }

            await _auditLogClient.CreateAuditLogAsync(auditLog);
        }
        catch (Exception ex)
        {
            // Don't let audit logging failures break the main operation
            _logger.LogError(ex, "Failed to create audit log for action: {Action}", auditLog.Action);
        }
    }

    /// <summary>
    /// Get client IP address from HTTP context
    /// </summary>
    private string? GetClientIpAddress()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            // Check for forwarded headers (useful when behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fallback to connection remote IP
            return context.Connection.RemoteIpAddress?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get client IP address");
            return null;
        }
    }

    /// <summary>
    /// Get user agent from HTTP context
    /// </summary>
    private string? GetUserAgent()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Request.Headers.UserAgent.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user agent");
            return null;
        }
    }
}