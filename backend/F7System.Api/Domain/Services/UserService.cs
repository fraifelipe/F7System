using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using F7System.Api.Domain.Commands;
using F7System.Api.Domain.Enums;
using F7System.Api.Domain.Models;
using F7System.Api.Infrastructure.Models;
using F7System.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace F7System.Api.Domain.Services
{
    public class UserService : IUserService
    {
        private readonly AppSettings _appSettings;
        private readonly F7DbContext _f7DbContext;

        public UserService(IOptions<AppSettings> appSettings, F7DbContext f7DbContext)
        {
            _appSettings = appSettings.Value;
            _f7DbContext = f7DbContext;
        }

        public PessoaUsuario Authenticate(LoginModel loginModel)
        {
            if (string.IsNullOrEmpty(loginModel.Username) || string.IsNullOrEmpty(loginModel.Password))
                throw new Exception("Usuario ou senha incorretos");

            var user = _f7DbContext.PessoaUsuarioDbSet.FirstOrDefault(x => x.Username == loginModel.Username);

            if (user == null)
                throw new Exception("Usuario incorreto");
 
            if (!VerifyPasswordHash(loginModel.Password, user.PasswordHash, user.PasswordSalt))
                throw new Exception("Senha incorreta");
            
            user.Token = GenerateToken(user);
            return user;
        }

        public void GiveAccess(PessoaUsuario pessoaUsuario, LoginModel loginModel)
        {
            pessoaUsuario.Username = loginModel.Username;

            if(_f7DbContext.PessoaUsuarioDbSet.FirstOrDefault(x => x.Username == pessoaUsuario.Username) != null)  
                throw new Exception("Username \"" + pessoaUsuario.Username + "\" is already taken");

            CreatePasswordHash(loginModel.Password, out var passwordHash, out var passwordSalt);
            pessoaUsuario.PasswordHash = passwordHash;
            pessoaUsuario.PasswordSalt = passwordSalt;
            
            _f7DbContext.SaveChanges();
        }

        public IQueryable<PessoaUsuario> GetAll()
        {
            return _f7DbContext.PessoaUsuarioDbSet;
        }

        public PessoaUsuario GetById(Guid id)
        {
            return _f7DbContext.PessoaUsuarioDbSet.Find(id);
        }

        public void Update(UpdateUserCommand cmd)
        {
            var user = _f7DbContext.PessoaUsuarioDbSet.Find(cmd.UserId);

            if (user == null)
                throw new Exception("User not found");

            if (cmd.Username != user.Username && (_f7DbContext.PessoaUsuarioDbSet.FirstOrDefault(x => x.Username == user.Username) != null)){
                throw new Exception("Username " + cmd.Username + " is already taken");
            }
            
            user.Username = cmd.Username;

            // update password if it was entered
            if (!string.IsNullOrWhiteSpace(cmd.Password))
            {
                CreatePasswordHash(cmd.Password, out var passwordHash, out var passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
            }
            
            _f7DbContext.Update(user);
            _f7DbContext.SaveChanges();
        }

        public void Delete(Guid id)
        {
            var user = _f7DbContext.PessoaUsuarioDbSet.Find(id);
            if (user != null)
            {
                _f7DbContext.Remove(user);
                _f7DbContext.SaveChanges();
            }
        }

        private string GenerateToken(PessoaUsuario pessoaUsuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new []
                {
                    new Claim(ClaimTypes.NameIdentifier, pessoaUsuario.Id.ToString()),
                    new Claim(ClaimTypes.Role, pessoaUsuario.Perfil.ToString()),
                    new Claim(ClaimTypes.Name, pessoaUsuario.Username)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        
        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new Exception("Password is required");
            }

            using var hmac = new System.Security.Cryptography.HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (storedHash?.Length != 64 || storedSalt?.Length != 128)
            {
                return false;
            }

            using var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            if (computedHash.Where((t, i) => t != storedHash[i]).Any())
            {
                return false;
            }

            return true;
        }

        public void CreateAdminUserWhenDontHaveManagerUsers()
        {
            if (!_f7DbContext.PessoaUsuarioDbSet.Any())
            {
                var admin = new PessoaUsuario();
                
                GiveAccess(admin, new LoginModel()
                {
                    Username = "admin",
                    Password = "admin"
                });

                admin.Perfil = Perfil.Administrator;
                // user.Manager = new Manager(){UserId = user.UserId};
                // _f7DbContext.ManagerDbSet.Add(user.Manager);
                _f7DbContext.Add(admin);
                _f7DbContext.SaveChanges();
            }
        }
    }
}