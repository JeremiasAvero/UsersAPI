using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Dapper;
using DotnetApi.Data;
using DotnetApi.Dtos;
using DotnetApi.Helpers;
using DotnetApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;

namespace DotnetApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly DataContextDapper _dapper;
        private readonly AuthHelper _authHelper;
        private readonly ReusableSQL _reusableSql;
        private readonly IMapper _mapper;

        public AuthController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
            _authHelper = new AuthHelper(config);
            _reusableSql = new ReusableSQL(config);
            _mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserForRegistrationDto, UserComplete>();
            }));
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public IActionResult Register(UserForRegistrationDto userForRegistration)
        {
            if (userForRegistration.Password == userForRegistration.PasswordConfirm)
            {
                string sqlCheckUserExits = $"SELECT Email FROM TutorialAppSchema.Auth WHERE Email = '{userForRegistration.Email}'";

                IEnumerable<string> existingUsers = _dapper.LoadData<string>(sqlCheckUserExits);
                if (existingUsers.Count() == 0)
                {

                    UserForLoginDto userForLogin = new UserForLoginDto()
                    {
                        Email = userForRegistration.Email,
                        Password = userForRegistration.Password
                    };
                    if (_authHelper.SetPassword(userForLogin))
                    {
                        UserComplete user = _mapper.Map<UserComplete>(userForRegistration);
                        user.Active = true;

                        if (_reusableSql.UpsertUser(user))
                        {
                            return Ok();

                        }
                        throw new Exception("Failed to register user!");

                    }

                    throw new Exception("Failed to register user!");
                }
                throw new Exception("Email already in use!");
            }
            throw new Exception("Passwords do not match!");
        }

        [HttpPut("ResetPassword")]
        public IActionResult ResetPassword(UserForLoginDto userForSetPassword)
        {
            if (_authHelper.SetPassword(userForSetPassword))
            {
                return Ok();
            }

            throw new Exception("Failed to set password");
        }
        [AllowAnonymous]
        [HttpPost("Login")]
        public IActionResult Login(UserForLoginDto userForLogin)
        {
            string sqlForHashAndSalt = @"EXEC TutorialAppSchema.spLoginConfirmation_Get 
                @Email = @EmailParam";

            DynamicParameters sqlParameters = new DynamicParameters();


            sqlParameters.Add("@EmailParam", userForLogin.Email, DbType.String);

            UserForLoginConfirmationDto userForConfirmation = _dapper
                .LoadDataSingleWithParameters<UserForLoginConfirmationDto>(sqlForHashAndSalt, sqlParameters);

            byte[] passwordHash = _authHelper.GetPasswordHash(userForLogin.Password, userForConfirmation.PasswordSalt);

            for (int i = 0; i < passwordHash.Length; i++)
            {
                if (passwordHash[i] != userForConfirmation.PasswordHash[i])
                {
                    return StatusCode(401, "Incorrect password!");
                }
            }

            string userIdSql = @$"
            SELECT 
            UserId 
            FROM TutorialAppSchema.Users 
            WHERE Email = '{userForLogin.Email}' ";

            int userId = _dapper.LoadDataSingle<int>(userIdSql); ;

            return Ok(new Dictionary<string, string>{
                {"token", _authHelper.CreateToken(userId)}
            });
        }

        [HttpGet("RefreshToken")]
        public string RefreshToken()
        {
            string userIdSql = @$"
            SELECT 
            UserId 
            FROM TutorialAppSchema.Users 
            WHERE UserId = '{User.FindFirst("userId")?.Value}' ";

            int userId = _dapper.LoadDataSingle<int>(userIdSql);

            return _authHelper.CreateToken(userId);
        }

    }
}