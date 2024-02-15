using System.Data;
using Dapper;
using DotnetApi.Data;
using DotnetApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetApi.Controllers
{

    // [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PostController : ControllerBase
    {

        private readonly DataContextDapper _dapper;
        public PostController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("Post/{userId}/{postId}/{searchParam}")]
        public IEnumerable<Post> GetPosts(int userId = 0, int postId = 0, string searchParam = "none")
        {
            string sql = @$"EXEC TutorialAppSchema.spPosts_Get ";
            string stringParameters = "";
            DynamicParameters sqlParameters = new DynamicParameters();
            if (postId != 0)
            {
                stringParameters += @$", @PostId = @PostIdParam";
                sqlParameters.Add("@PostIdParam", postId, DbType.Int32);

            }


            if (userId != 0)
            {
                stringParameters += @$", @UserId = @UserIdParam";
                sqlParameters.Add("@UserIdParam", userId, DbType.Int32);
            }


            if (searchParam.ToLower() != "none")
            {
                stringParameters += @$", @SearchValue= @SearchValueParam";
                sqlParameters.Add("@SearchValueParam", searchParam.ToLower(), DbType.String);

            }


            if (stringParameters.Length > 0)
            {
                sql += stringParameters.Substring(1);

            }

            return _dapper.LoadDataWithParameters<Post>(sql, sqlParameters);

        }



        [HttpGet("MyPosts")]
        public IEnumerable<Post> GetMyPost()
        {
            string sql = @$"EXEC TutorialAppSchema.spPosts_Get @UserId = {this.User.FindFirst("userId")?.Value}";

            return _dapper.LoadData<Post>(sql);

        }

        [HttpPut("Post")]
        public IActionResult UpsertPost(Post post)
        {

            DynamicParameters sqlParameters = new DynamicParameters();


            string sql = @$"EXEC TutorialAppSchema.spPosts_Upsert 
                @UserId = @UserIdParam
            , @PostTitle = @PostTitleParam
            , @PostContent = @PostContentParam";

            sqlParameters.Add("@UserIdParam", this.User.FindFirst("userId")?.Value, DbType.Int32);
            sqlParameters.Add("@PostTitleParam", post.PostTitle, DbType.Int32);
            sqlParameters.Add("@PostContentParam", post.PostContent, DbType.Int32);

            if (post.PostId > 0)
            {
                sql += $", @PostId = @PostIdParam";
                sqlParameters.Add("@PostIdParam", post.PostId, DbType.Int32);

            }

            if (_dapper.ExecuteSqlWithParameters(sql, sqlParameters))
            {
                return Ok();
            }


            throw new Exception("Failed to upsert new post!");
        }


        [HttpDelete("Post")]
        public IActionResult DeletePost(int postId)
        {
            string sql = @$"EXEC TutorialAppSchema.spPost_Delete @UserId = @UserIdParam, @PostId = PostIdParam";

            DynamicParameters sqlParameters = new DynamicParameters();

            sqlParameters.Add("@UserIdParam", this.User.FindFirst("userId")?.Value, DbType.Int32);
            sqlParameters.Add("@PostIdParam", postId.ToString(), DbType.Int32);

            if (_dapper.ExecuteSqlWithParameters(sql, sqlParameters))
            {
                return Ok();
            }

            throw new Exception("Failed to delete post!");
        }
    }
}