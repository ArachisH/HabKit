﻿using System.Numerics;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace HabBit.Commands
{
    public class RSACommand : Command
    {
        public string Modulus { get; set; }
        public string Exponent { get; set; }
        public string PrivateExponent { get; set; }

        public RSACommand()
        {
            Exponent = "3";
            Modulus = "86851dd364d5c5cece3c883171cc6ddc5760779b992482bd1e20dd296888df91b33b936a7b93f06d29e8870f703a216257dec7c81de0058fea4cc5116f75e6efc4e9113513e45357dc3fd43d4efab5963ef178b78bd61e81a14c603b24c8bcce0a12230b320045498edc29282ff0603bc7b7dae8fc1b05b52b2f301a9dc783b7";
            PrivateExponent = "59ae13e243392e89ded305764bdd9e92e4eafa67bb6dac7e1415e8c645b0950bccd26246fd0d4af37145af5fa026c0ec3a94853013eaae5ff1888360f4f9449ee023762ec195dff3f30ca0b08b8c947e3859877b5d7dced5c8715c58b53740b84e11fbc71349a27c31745fcefeeea57cff291099205e230e0c7c27e8e1c0512b";
        }

        public override void Populate(Queue<string> parameters)
        {
            if (parameters.Count == 1)
            {
                var keySize = int.Parse(parameters.Dequeue());
                using (var rsa = new RSACryptoServiceProvider(keySize))
                {
                    RSAParameters rsaKeys = rsa.ExportParameters(true);
                    Modulus = ToHex(rsaKeys.Modulus);
                    Exponent = ToHex(rsaKeys.Exponent);
                    PrivateExponent = ToHex(rsaKeys.D);
                }
            }
            else
            {
                Exponent = parameters.Dequeue();
                Modulus = parameters.Dequeue();
                PrivateExponent = null;
            }
        }

        private string ToHex(byte[] data)
        {
            return new BigInteger(ReverseNull(data)).ToString("x");
        }
        private byte[] ReverseNull(byte[] data)
        {
            bool isNegative = false;
            int newSize = data.Length;
            if (data[0] > 127)
            {
                newSize += 1;
                isNegative = true;
            }

            var reversed = new byte[newSize];
            for (int i = 0; i < data.Length; i++)
            {
                reversed[i] = data[data.Length - (i + 1)];
            }
            if (isNegative)
            {
                reversed[reversed.Length - 1] = 0;
            }
            return reversed;
        }
    }
}