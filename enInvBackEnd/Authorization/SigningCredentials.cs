using Microsoft.IdentityModel.Tokens;

namespace enInvBackEnd.Authorization
{
    internal class SigningCredentials
    {
        private SymmetricSecurityKey symmetricSecurityKey;
        private object hmacSha256Signature;
        private SymmetricSecurityKey symmetricSecurityKey1;

        public SigningCredentials(SymmetricSecurityKey symmetricSecurityKey, object hmacSha256Signature)
        {
            this.symmetricSecurityKey = symmetricSecurityKey;
            this.hmacSha256Signature = hmacSha256Signature;
        }

        public SigningCredentials(SymmetricSecurityKey symmetricSecurityKey1, string hmacSha256Signature)
        {
            this.symmetricSecurityKey1 = symmetricSecurityKey1;
            this.hmacSha256Signature = hmacSha256Signature;
        }
    }
}