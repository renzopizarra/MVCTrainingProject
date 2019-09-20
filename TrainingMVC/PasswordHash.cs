using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace TrainingMVC
{
    public static class PasswordHash
    {
        public static string Hash(string value)
        {
            //Used SHA-2 Algorithm or SHA-256 specifically
            return Convert.ToBase64String(System.Security.Cryptography.SHA256.Create()
                .ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
    }
}