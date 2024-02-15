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
    public class TestController : ControllerBase
    {
        private readonly DataContextDapper _dapper;

        public TestController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);

        }

        [HttpGet("Connection")]
        public DateTime TestConnection()
        {
            return _dapper.LoadDataSingle<DateTime>("SELECT GETDATE()");
        }

        [HttpGet]
        public string Test()
        {
            return "Your application is running";
        }



    }
}