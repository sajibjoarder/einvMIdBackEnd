using enInvBackEnd.DataContext;
using enInvBackEnd.DataModels;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;


namespace enInvBackEnd.Authorization
{
    public class TokenGenerator
    {
        public async Task<User?> CheckLogin(string email, string inputPassword)
        {
            using (var dbcontext = new EninvContext())
            {
                var user = await dbcontext.Users.FirstOrDefaultAsync(x => x.Email == email);

                if (user != null && VerifyPassword(inputPassword, user.PasswordHash))
                {
                    //string token = await Task.Run(() => GenerateToken(user.Id, user.Email));
                    return user;
                }

                return null; 
            }
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
        }

        public string GenerateToken(Guid userID, string email,string role)
        {

            var tokenHandler = new JwtSecurityTokenHandler();

            var key = "asdklajoi21ioe21lk321lk3w21kl3sdsaas"u8.ToArray();
           
            var has_lhdn = false;

            using (EninvContext DBcontext = new EninvContext())
            {
                var result = DBcontext.CompanyUsers
                    .Where(cu => cu.UserId == userID)
                    .Join(DBcontext.CompanyDetails, cu => cu.CompanyId, cd => cd.CompanyId, (cu, cd) => new { cu, cd })
                    .Join(DBcontext.LhdnProfiles, temp => temp.cd.CompanyId, lp => lp.CompanyId, (temp, lp) => new { temp.cu, temp.cd, lp })
                    .Where(x => x.lp.ClientIdLhdn != null && x.lp.ClientSecretLhdn != null)
                    .Select(x => new
                    {
                        x.cu.UserId,
                        x.cu.CompanyId,
                        x.cd.CompanyName,
                        x.lp.ClientIdLhdn,
                        x.lp.ClientSecretLhdn
                    })
                    .FirstOrDefault();

                if (result != null)
                {
                    has_lhdn = true;
                }
              
            }



            var claims = new ClaimsIdentity(new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sid, userID.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("has_lhdn", has_lhdn.ToString())
            });

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Expires = DateTime.UtcNow.AddMinutes(1),
                Issuer = "zam sdn bhd",
                Audience = "zam sdn bhd",
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)

            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

    }
}
