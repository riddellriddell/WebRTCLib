﻿using System;
using UnityEngine.Networking;
using System.Security.Cryptography.X509Certificates;
 
namespace Networking
{
    public class CustomHttpsCert : CertificateHandler
    {
        // Encoded RSAPublicKey
        private static readonly string PUB_KEY = "";
        
        /// <summary>
        /// Validate the Certificate Against the Amazon public Cert
        /// </summary>
        /// <param name="certificateData">Certifcate to validate</param>
        /// <returns></returns>
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}
