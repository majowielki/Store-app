using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using store_app.API.Data;
using store_app.API.Models.Dto;
using store_app.API.Models;
using store_app.API.Utility;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace store_app.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private ApiResponse _apiResponse;
        private string secretKey;
        public AuthController(ApplicationDbContext dbContext, IConfiguration configuration, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _dbContext = dbContext;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _apiResponse = new ApiResponse();
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            ApplicationUser userFromDb = _dbContext.ApplicationUsers
                    .FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower());

            bool isValid = await _userManager.CheckPasswordAsync(userFromDb, model.Password);

            if (isValid == false)
            {
                _apiResponse.Result = new LoginResponseDto();
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("Email or password is incorrect");
                return BadRequest(_apiResponse);
            }

            JwtSecurityTokenHandler tokenHandler = new();
            byte[] key = Encoding.ASCII.GetBytes(secretKey);

            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("fullName", userFromDb.UserName),
                    new Claim("id", userFromDb.Id),
                    new Claim(ClaimTypes.Email, userFromDb.Email)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

            LoginResponseDto loginResponse = new()
            {
                UserName = userFromDb.UserName,
                Token = tokenHandler.WriteToken(token)
            };

            if (loginResponse.UserName == null || string.IsNullOrEmpty(loginResponse.Token))
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("UserName is invalid");
                return BadRequest(_apiResponse);
            }

            _apiResponse.StatusCode = HttpStatusCode.OK;
            _apiResponse.IsSuccess = true;
            _apiResponse.Result = loginResponse;
            return Ok(_apiResponse);

        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto model)
        {
            ApplicationUser userFromDb = _dbContext.ApplicationUsers
                .FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower());

            if (userFromDb != null)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("This email is already registered");
                return BadRequest(_apiResponse);
            }

            ApplicationUser newUser = new()
            {
                UserName = model.UserName,
                Email = model.Email
            };

            try
            {
                var result = await _userManager.CreateAsync(newUser, model.Password);
                if (result.Succeeded)
                {
                    if (!_roleManager.RoleExistsAsync(Const.Role_Admin).GetAwaiter().GetResult())
                    {
                        await _roleManager.CreateAsync(new IdentityRole(Const.Role_Admin));
                        await _roleManager.CreateAsync(new IdentityRole(Const.Role_User));
                    }
                    await _userManager.AddToRoleAsync(newUser, Const.Role_User);
                    _apiResponse.StatusCode = HttpStatusCode.OK;
                    _apiResponse.IsSuccess = true;
                    return Ok(_apiResponse);
                }
            }
            catch (Exception)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("Error while registering");
                return BadRequest(_apiResponse);
            }
            _apiResponse.StatusCode = HttpStatusCode.BadRequest;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages.Add("Error while registering");
            return BadRequest(_apiResponse);

        }
    }
}
