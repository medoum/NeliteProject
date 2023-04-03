using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.RegularExpressions;
using Backend.Context;
using Backend.Helpers;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using Backend.Models.Dto;
using Backend.UtilityService;

namespace Backend.Controllers
{
    // The endpoint
    [Route("api/[controller]")]
    [ApiController]

    // The class controller
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _authContext;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        public UserController(AppDbContext appDbContext, IConfiguration configuration, IEmailService emailService)
        {

            _authContext = appDbContext;
            _configuration = configuration;
            _emailService = emailService;
        }

        // The logic for login
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
            // If the user object is null
            if (userObj == null)
                return BadRequest();
            // checking if the user has the same username and the same password 
            var user = await _authContext.Users
                .FirstOrDefaultAsync(x => x.Username == userObj.Username);
            // if not found
            if (user == null)
                return NotFound(new { Message = "User Not Found!" });

            // Check if the username exist or not

            if (PasswordHasher.VerifyPassword(userObj.Password, user.Password))
            {
                return BadRequest(new { Message = "Password is Incorrect" });
            }

            user.Token = CreateJwt(user);
            var newAccessToken = user.Token;
            var newRefreshToken = CreateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(5);
            await _authContext.SaveChangesAsync();


            // if it is found
            return Ok(new TokenApiDto()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }
        // The logic for the register
        [HttpPost("register")]

        public async Task<IActionResult> AddUser([FromBody] User userObj)
        {
            // Checking if the user object is null
            if (userObj == null)
                return BadRequest();

            //Check if username exist
            if (await CheckUsernameExistAsync(userObj.Username))
                return BadRequest(new { Message = "Username Already Exist!" });

            // Check if Email exist
            if (await CheckEmailExistAsync(userObj.Email))
                return BadRequest(new { Message = "Email Already Exist!" });

            //Check password Strength
            var pass = CheckPasswordStrength(userObj.Password);
            if (!string.IsNullOrEmpty(pass))
                return BadRequest(new { Message = pass.ToString() });
            // Hashing the password before sending to the data
            userObj.Password = PasswordHasher.HashPassword(userObj.Password);

            //Defining the role
            userObj.Role = "User";

            //Defining the token
            userObj.Token = "";

            // if the user object has a value or not null
            await _authContext.Users.AddAsync(userObj);
            await _authContext.SaveChangesAsync();
            return Ok(new
            {
                Message = "User Registered"
            });

        }

        // The method checking if user exist or not
        private Task<bool> CheckUsernameExistAsync(string? username)
            => _authContext.Users.AnyAsync(x => x.Username == username);

        // The method checking if the email exist or not
        private Task<bool> CheckEmailExistAsync(string? email)
            => _authContext.Users.AnyAsync(x => x.Email == email);

        // The method checking the password strength
        private string CheckPasswordStrength(string pass)
        {
            // Checking if the password is less than 8
            StringBuilder sb = new StringBuilder();
            if (pass.Length < 9)
                sb.Append("Minimum password length should be 8" + Environment.NewLine);
            // Checking if the password is alphanumeric
            if (!(Regex.IsMatch(pass, "[a-z]") && Regex.IsMatch(pass, "[A-Z]") && Regex.IsMatch(pass, "[0-9]")))
                sb.Append("Password should be Alphanenumeric" + Environment.NewLine);
            return sb.ToString();
        }

        // The token method
        private string CreateJwt(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("veryverysecret.....");
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name,$"{user.Username}")
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = credentials
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }

        // Refresh Method token
        private string CreateRefreshToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var refreshToken = Convert.ToBase64String(tokenBytes);

            var tokenInUser = _authContext.Users
                .Any(a => a.RefreshToken == refreshToken);

            if (tokenInUser)
            {
                return CreateRefreshToken();
            }
            return refreshToken;
        }

        private ClaimsPrincipal GetPrincipleFromExpiredToken(string token)
        {
            var key = Encoding.ASCII.GetBytes("veryverysecret.....");
            var tokenvalidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenvalidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("this is Invalid token");
            return principal;


        }

        // Get all users
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<User>> GetAllUsers()
        {
            return Ok(await _authContext.Users.ToListAsync());
        }

        // Create a refresh token

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenApiDto tokenApiDto)
        {
            if (tokenApiDto is null)
                return BadRequest("Invalid Client Request");
            string accesToken = tokenApiDto.AccessToken;
            string refreshToken = tokenApiDto.RefreshToken;
            var principal = GetPrincipleFromExpiredToken(accesToken);
            var username = principal.Identity.Name;
            var user = await _authContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
                return BadRequest("Invalid Request");
            var newAccesToken = CreateJwt(user);
            var newRefreshToken = CreateRefreshToken();
            user.RefreshToken = newRefreshToken;
            await _authContext.SaveChangesAsync();
            return Ok(new TokenApiDto()
            {
                AccessToken = newAccesToken,
                RefreshToken = newRefreshToken,
            });
        }
        // Reset email endpoint
        [HttpPost("send-reset-email/{email}")]
        public async Task<IActionResult> SendEmail(string email)
        {
            var user = await _authContext.Users.FirstOrDefaultAsync(a => a.Email == email);
            if(user is null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "email Doesn't Exist"
                });
            }
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var emaiToken = Convert.ToBase64String(tokenBytes);
            user.ResetPasswordToken = emaiToken;
            user.ResetPasswordExpiry = DateTime.Now.AddMinutes(15);
            string? from = _configuration["EmailSettings:From"];
            var emailModel = new EmailModel(email, "Reset Password!!", EmailBody.EmailStringBody(email, emaiToken));
            _emailService.SendEmail(emailModel);
            _authContext.Entry(user).State = EntityState.Modified;
            await _authContext.SaveChangesAsync();
            return Ok(new
            {
                StatusCode = 200,
                Message = "Email Sent!"
            });
        }
        // Reset password endpoint

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var newToken = resetPasswordDto.EmailToken.Replace(" ", "+");
            var user = await _authContext.Users.AsNoTracking().FirstOrDefaultAsync(a => a.Email == resetPasswordDto.Email);
            if (user is null)
            {
                return NotFound(new
                {
                    StatusCode = 404,
                    Message = "User Doesn't Exist"
                });
            }
            var tokenCode = user.ResetPasswordToken;
            DateTime emailTokenExpiry = user.ResetPasswordExpiry;
            if(tokenCode != resetPasswordDto.EmailToken || emailTokenExpiry < DateTime.Now)
            {
                return BadRequest(new
                {
                    StatusCode = 400,
                    Message = "Invalid Reset link"
                });
            }
            user.Password = PasswordHasher.HashPassword(resetPasswordDto.NewPassword);
            _authContext.Entry(user).State = EntityState.Modified;
            await _authContext.SaveChangesAsync();
            return Ok(new
            {
                StatusCode = 200,
                Message = "Password reset successfully"
            });
        }
    }

}
 