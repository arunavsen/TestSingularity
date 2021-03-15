using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using TestApi.Models;
using TestApi.Models.DTO;

namespace TestApi.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly AppSettings _appSettings;

        public UserController(ApplicationDbContext db, IOptions<AppSettings> appSettings)
        {
            _db = db;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] User model)
        {
            var user = _db.Users.SingleOrDefault(u => u.UserName == model.UserName && u.Password == model.Password);

            //user is not found

            if (user == null)
            {
                return BadRequest(new { message = "Username or Password is incorrect" });
            }

            //if user is found generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);

            SecurityTokenDescriptor tokenDescriptor;

            if (user.Role != null)
            {
                tokenDescriptor = new SecurityTokenDescriptor()
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Name, user.Id.ToString()),
                        new Claim(ClaimTypes.Role, user.Role),
                    }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
            }
            else
            {
                tokenDescriptor = new SecurityTokenDescriptor()
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Name, user.Id.ToString())
                    }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
            }


            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);
            user.Password = "";

            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register([FromBody] User model)
        {
            var user = _db.Users.SingleOrDefault(u => u.UserName == model.UserName);
            var isUserNameUnique = user == null ? true : false;

            if (!isUserNameUnique)
            {
                return BadRequest(new {Message = "Username already exist"});
            }

            var u = new User()
            {
                UserName = model.UserName,
                Password = model.Password,
                Role = model.Role
            };

            _db.Users.Add(u);
            _db.SaveChanges();

            u.Password = "";

            return Ok(u);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUser()
        {
            return await _db.Users.ToListAsync();
        }

        [HttpPost("CreateUser")]
        public IActionResult CreateUser([FromBody] RegisterDto model)
        {
            var user = _db.Users.SingleOrDefault(m => m.UserName == model.UserName);

            var isUserUnique = user == null ? true : false;

            if (!isUserUnique)
            {
                return BadRequest("Username is already exist");
            }

            var u = new User()
            {
                UserName = model.UserName,
                Password = model.Password,
                Role = model.Role
            };

            _db.Users.Add(u);
            _db.SaveChanges();

            model.Password = "";
            return Ok(u);
        }

        [HttpDelete("RemoveUser/{id}")]
        public async Task<ActionResult<User>> RemoveUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }


            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return user;

        }

    }
}