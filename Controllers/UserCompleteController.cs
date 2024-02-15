using System.Data;
using Dapper;
using DotnetApi.Data;
using DotnetApi.Dtos;
using DotnetApi.Helpers;
using DotnetApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DotnetApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly DataContextDapper _dapper;
        private readonly ReusableSQL _reusableSql;
        public UserController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
            _reusableSql = new ReusableSQL(config);
        }

        [HttpGet("Test")]
        public DateTime TestConnection()
        {
            return _dapper.LoadDataSingle<DateTime>("SELECT GETDATE()");
        }
        [HttpGet("GetUsers/{userId}/{isActive}")]
        public IEnumerable<UserComplete> GetUsers(int userId, bool isActive)
        {
            string sql = $@"EXEC TutorialAppSchema.spUsers_Get ";
            string stringParameters = "";
            DynamicParameters sqlParameters = new DynamicParameters();
            if (userId != 0)
            {
                stringParameters += $", @UserId = @UserIdParam";
                sqlParameters.Add("@UserIdParam", userId, DbType.Int32);
            }
            if (isActive)
            {
                stringParameters += ", @Active = @ActiveParam";
                sqlParameters.Add("@ActiveParam", isActive, DbType.Boolean);
            }
            if (stringParameters.Length > 0)
            {
                sql += stringParameters.Substring(1);

            }
            IEnumerable<UserComplete> users = _dapper.LoadDataWithParameters<UserComplete>(sql, sqlParameters);
            return users;
        }

        [HttpPut("UpsertUser")]
        public IActionResult UpsertUser(UserComplete user)
        {
            if (_reusableSql.UpsertUser(user))
            {
                return Ok();
            }

            throw new Exception("Failed to Update User");
        }

        [HttpDelete("DeleteUser/{userId}")]
        public IActionResult DeleteUser(int userId)
        {
            string sql = @$"EXEC TutorialAppSchema.spUser_Delete
    @UserId = @UserIdParam";
            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@UserIdParam", userId, DbType.Int32);
            return _dapper.ExecuteSqlWithParameters(sql, sqlParameters) ? Ok() : NotFound();
        }


    }
}