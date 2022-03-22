using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MissionPlanner.Mavlink;
using static MissionPlanner.Mavlink.MAVMemAuthKeys;

namespace MissionPlanner.Controls
{
    class Login
    {

        //::prot::public static string currUserFile = Settings.GetUserDataDirectory() + "passHashSaltNames.xml";
        public static string currUserFile = "UsernamesDataAndPass.xml";


        public static int _numIter = 50000;


        //decalre properties 
        public static string Username { get; set; }
        public static byte[] UserpasswordHash { get; set; }
        public static byte[] User_pbkdf_k1 { get; set; }
        public static byte[] User_pbkdf_k2 { get; set; }

        //intialise  
        public Login(string user, byte[] pass)
        {
            Username = user;
            //::!::UserpasswordHash = pass;
        }


        static int pbkdf_size = 32;  //%40
        static int vi_size = 16;  //%40
        public static void signup(string user, string pass)
        {


            //since signup generate random Salt to be saved
            byte[] salt = GenerateSalt();

            Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(pass, salt, Login._numIter);// ::os::  availble in version 4.3, HashAlgorithmName.SHA256);


            //create PBKDF key
            byte[] PBKDF_Hash = rfc2898.GetBytes(pbkdf_size * 2);

            byte[] PBKDF_K1 = PBKDF_Hash.Take(pbkdf_size).ToArray();
            byte[] PBKDF_K2 = PBKDF_Hash.Skip(pbkdf_size).ToArray().Take(pbkdf_size).ToArray();


            /// generate IV from different source
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            var IV = new byte[16];
            provider.GetBytes(IV);


            //before save, read old file if already registered
            bool userFound = LoadFile(user);
            // warning if user already there erased
            if (!userFound)
            {
                //store k1,k2 for this session 
                Login.User_pbkdf_k1 = PBKDF_K1;
                Login.User_pbkdf_k2 = PBKDF_K2;
                //Login.Keys = new AuthPasswords();
                //login = new Login(user, pass);
                Username = user;



                //Login.Keys[user] = new Login.PassPackage() { name = user, salt = salt, passHash = PBKDF_K1 };

                //remove old keys from ram
                MAVMemAuthKeys.currUserRecord = new MAVMemAuthKeys.UserRecord();
                MAVMemAuthKeys.currUserRecord.name = user;
                MAVMemAuthKeys.currUserRecord.salt = salt;
                MAVMemAuthKeys.currUserRecord.iv = IV;

                MAVMemAuthKeys.currUserRecord.dict = new Dictionary<string, AuthKey>();

                //wncrypt empty obj to bytes[] 
                MAVMemAuthKeys.currUserRecord.cipher = Encrypt(Login.User_pbkdf_k1, MAVMemAuthKeys.currUserRecord.iv, ObjectToByteArray(MAVMemAuthKeys.currUserRecord.dict)); //convert obj to plain bytes then encrypt
                MAVMemAuthKeys.currUserRecord.tag = getTag(MAVMemAuthKeys.currUserRecord.cipher, Login.User_pbkdf_k2);

                SaveAndEncryptDataOnly();
                //load embedded mem keys
                //LoadKeys();

               MessageBox.Show("signup success");
            }
            else
            {

                MessageBox.Show("user already signed up!");
            }

        }



        public static bool verifyPass(string user, string pass)
        {
            bool userFound = LoadFile(user);


            try
            {
                //PassPackage unverifiedNamePack = Login.Keys[user];
                if (userFound) //found and loaded currUserRecord but NOT yet VERIFIED
                {
                    //if user name found generate hash from pass (using same stored salt)
                    // calc PBKDF and try to decrypt
                    Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(pass, MAVMemAuthKeys.currUserRecord.salt, Login._numIter);// ::os::  availble in version 4.3, HashAlgorithmName.SHA256);

                    //create PBKDF key
                    byte[] PBKDF_Hash = rfc2898.GetBytes(pbkdf_size * 2);

                    byte[] PBKDF_K1 = PBKDF_Hash.Take(pbkdf_size).ToArray();
                    byte[] PBKDF_K2 = PBKDF_Hash.Skip(pbkdf_size).ToArray().Take(pbkdf_size).ToArray();



                    //byte[] iv_128bit = currUserRecord.iv.Take(16).ToArray();



                    //authenticate  ::fix:: is this authentication correct? ::?::
                    var claimedTag = getTag(MAVMemAuthKeys.currUserRecord.cipher, PBKDF_K2);
                    if (ByteArrayCompare(MAVMemAuthKeys.currUserRecord.tag, claimedTag)) //compare claimed from read tag
                    {
                        //now log in and store user info 
                        //login = new Login(user, PBKDF_K1);
                        Username = user;

                        //store k1,k2 for this session 
                        Login.User_pbkdf_k1 = PBKDF_K1;
                        Login.User_pbkdf_k2 = PBKDF_K2;


                        MAVMemAuthKeys.currUserRecord.name = user;
                        //MAVMemAuthKeys.currUserRecord.iv = IV;
                        //MAVMemAuthKeys.currUserRecord.tag = getTag(cipher,PBKDF_K2);



                        //// we know he is the right user, decrypt
                        byte[] dictBytes = Decrypt(User_pbkdf_k1, MAVMemAuthKeys.currUserRecord.iv, MAVMemAuthKeys.currUserRecord.cipher); //convert obj to plain bytes then encrypt
                        MAVMemAuthKeys.currUserRecord.dict = (Dictionary<string, AuthKey>)ByteArrayToObject(dictBytes);
                        if (MAVMemAuthKeys.currUserRecord.dict == null)
                        {  //if try to decrypt and failed (e.g signup with same name diff pass will make decryption fail (null)
                            MAVMemAuthKeys.currUserRecord.dict = new Dictionary<string, AuthKey>();
                        }
                        // MAVMemAuthKeys.currUserRecord.tag = getTag(MAVMemAuthKeys.currUserRecord.cipher, PBKDF_K2);
                        //MAVMemAuthKeys.currUserRecord.cipher = null; //to not be stored as plain ::fix:: do it cleaner


                        return true;
                    }
                }

            }
            catch (Exception ex)
            {
                return false;
            }


            return false;
        }

        public static bool LoadFile(string user)
        {
            //Login.currUserFile = user;
            currUserFile = user + ".xml";
            if (!File.Exists(currUserFile))
                return false;
            try
            {


                DataContractSerializer reader =
                    new DataContractSerializer(typeof(UserRecord));//,
                                                                   // new Type[] { typeof(AuthKey) });



                //TripleDES encAlg = TripleDES.Create();
                using (var fs = new FileStream(currUserFile, FileMode.Open))
                //using (Rijndael rijAlg = Rijndael.Create())
                {

                    //::prot::Keys = (AuthKeys)reader.ReadObject(sr);
                    MAVMemAuthKeys.currUserRecord = (UserRecord)reader.ReadObject(fs);
                    //::prot::writer.WriteObject(sw, Keys);
                    //writer.WriteObject(sw, Keys);
                }

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return false;
                //::prot::log.Error(ex);
            }
        }

        /*
        //internal static void Load() //::aes:: I made this public to force call it from UI since not called
        public static bool LoadEncryptedFile(string user)
        {
            //Login.currUserFile = user;
            currUserFile = user + ".xml";
            if (!File.Exists(currUserFile))
                return false;
            try
            {


                DataContractSerializer reader =
                    new DataContractSerializer(typeof(UserRecord));//,
                                                                   // new Type[] { typeof(AuthKey) });


                ////////////////////
                ///

                using (var fs = new FileStream(currUserFile, FileMode.Open))
                using (var rijCrypto = new RijndaelManaged())
                {
                    byte[] encryptedData;
                    rijCrypto.Padding = System.Security.Cryptography.PaddingMode.ISO10126;
                    rijCrypto.KeySize = 256;


                    var decryptor = rijCrypto.CreateDecryptor();

                    using (var csDecrypt = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
                    {
                        

                        byte[] encBytes = new byte[fs.Length];
                        //OutputFile.Write(new  StreamReader(csDecrypt).ReadToEnd());

                        int data;
                        int i = 0;
                        while (-1 != (data = fs.ReadByte()))
                        {
                            //csDecrypt.WriteByte((byte)data);
                            encBytes[i] = (byte)data;
                            i++;
                        }

                        //csDecrypt.Read( //( encBytes, 0, encBytes.Length);
                        //writer.WriteObject(csEncrypt, ObjectToByteArray(MAVMemAuthKeys.currUserRecord));
                        MAVMemAuthKeys.currUserRecord = (UserRecord)ByteArrayToObject(encBytes);
                    }

                }
                

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return false;
                //::prot::log.Error(ex);
            }
        }
        
        public static void SaveAndEncryptDataAndFile()
        {

            // update user keys data to be stored + calc cipher + tag + new IV

            //since data is modified, update IV with a new random value
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            var IV = new byte[16];
            provider.GetBytes(IV);
            MAVMemAuthKeys.currUserRecord.iv = IV;

            MAVMemAuthKeys.currUserRecord.cipher = Encrypt(Login.User_pbkdf_k1, MAVMemAuthKeys.currUserRecord.iv, ObjectToByteArray(currUserRecord.dict)); //convert obj to plain bytes then encrypt
            MAVMemAuthKeys.currUserRecord.tag = getTag(MAVMemAuthKeys.currUserRecord.cipher, Login.User_pbkdf_k2);
            var tempHolder = MAVMemAuthKeys.currUserRecord.dict;
            MAVMemAuthKeys.currUserRecord.dict = null; //to not be stored as plain ::fix:: do it cleaner


            // save config
            DataContractSerializer writer =
                new DataContractSerializer(typeof(UserRecord));//,
                                                               // new Type[] { typeof(AuthKey) });




            //TripleDES encAlg = TripleDES.Create();
            //using (var sw = new CryptoStream(fs, encAlg.CreateEncryptor(), CryptoStreamMode.Write))
            //::prot::using (var sw = new CryptoStream(fs, Rij.algorithm.CreateEncryptor(Rij.algorithm.Key, Rij.algorithm.IV), CryptoStreamMode.Write))

            using (var fs = new FileStream(Login.currUserFile, FileMode.Create))
            using (var rijCrypto = new RijndaelManaged())
            {
                byte[] encryptedData;
                rijCrypto.Padding = System.Security.Cryptography.PaddingMode.ISO10126;
                rijCrypto.KeySize = 256;


                var encryptor = rijCrypto.CreateEncryptor();

                using (var csEncrypt = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
                {
                    
                    //using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)){

                        //Write all data to the stream.
                    //    swEncrypt.Write(MAVMemAuthKeys.currUserRecord);                    }
                    //encrypted = msEncrypt.ToArray();
                    
                    var encBytes = ObjectToByteArray(MAVMemAuthKeys.currUserRecord);
                    csEncrypt.Write(encBytes, 0, encBytes.Length);
                    //writer.WriteObject(csEncrypt, ObjectToByteArray(MAVMemAuthKeys.currUserRecord));
                }

            }
            // using (Rijndael rijAlg = Rijndael.Create())

            MAVMemAuthKeys.currUserRecord.dict = tempHolder;
        }
    */
        public static void SaveAndEncryptDataOnly()
        {

            // update user keys data to be stored + calc cipher + tag + new IV

            //since data is modified, update IV with a new random value
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            var IV = new byte[16];
            provider.GetBytes(IV);
            MAVMemAuthKeys.currUserRecord.iv = IV;

            MAVMemAuthKeys.currUserRecord.cipher = Encrypt(Login.User_pbkdf_k1, MAVMemAuthKeys.currUserRecord.iv, ObjectToByteArray(currUserRecord.dict)); //convert obj to plain bytes then encrypt
            MAVMemAuthKeys.currUserRecord.tag = getTag(MAVMemAuthKeys.currUserRecord.cipher, Login.User_pbkdf_k2);
            var tempHolder = MAVMemAuthKeys.currUserRecord.dict;
            MAVMemAuthKeys.currUserRecord.dict = null; //to not be stored as plain ::fix:: do it cleaner


            // save config
            DataContractSerializer writer =
                new DataContractSerializer(typeof(UserRecord));//,
                                                               // new Type[] { typeof(AuthKey) });




            //TripleDES encAlg = TripleDES.Create();
            //using (var sw = new CryptoStream(fs, encAlg.CreateEncryptor(), CryptoStreamMode.Write))
            //::prot::using (var sw = new CryptoStream(fs, Rij.algorithm.CreateEncryptor(Rij.algorithm.Key, Rij.algorithm.IV), CryptoStreamMode.Write))

            using (var fs = new FileStream(Login.currUserFile, FileMode.Create))
            //using (var sw = new CryptoStream(fs, Rij.algorithm.CreateEncryptor(Rij.algorithm.Key, Rij.algorithm.IV), CryptoStreamMode.Write))

            // using (Rijndael rijAlg = Rijndael.Create())
            {
                //::prot::writer.WriteObject(sw, Keys);
                writer.WriteObject(fs, MAVMemAuthKeys.currUserRecord);
            }
            MAVMemAuthKeys.currUserRecord.dict = tempHolder;
        }


        /*
         * ::os:: need to add a reference in VS
        // byte[] is implicitly convertible to ReadOnlySpan<byte>
        static bool ByteArrayCompare(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
        {
            return a1.SequenceEqual(a2);
        }
        */

        //::os:: ::fix:: move to ByteArrayCompare(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)  + byte[] is implicitly convertible to ReadOnlySpan<byte>
        static bool ByteArrayCompare(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
                if (a1[i] != a2[i])
                    return false;

            return true;
        }


        private static byte[] getTag(byte[] cipher, byte[] pbkdf_k2)  //HMAC
        {
            using (var hmacsha256 = new HMACSHA256(pbkdf_k2))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(cipher);
                return hashmessage;
            }
        }
        public static byte[] GenerateSalt()
        {
            var salt = new byte[32];
            var randomProvider = new RNGCryptoServiceProvider();
            randomProvider.GetBytes(salt);

            return salt;
        }



    }
}

