using Amazon;
using Amazon.S3;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace common_tool
{
    public class Helpers
    {
        public static JObject GetHttpJson(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.ContentType = "application/json";
                request.Method = "GET";
                var response = (HttpWebResponse)request.GetResponse();
                var resStream = response.GetResponseStream();

                StreamReader streamRead = new StreamReader(resStream);
                string text = streamRead.ReadToEnd();
                return JObject.Parse(text);
            }
            catch (Exception e)
            {
                return null;
            }
        }


        public static void SetValue(JObject jObject, string key, string value)
        {
            if (jObject.ContainsKey(key) == true)
            {
                jObject[key] = value;
            }
            else
            {
                jObject.Add(key, value);
            }
        }


        public static JObject DownloadFromS3(string bucketName, string awsAccessKeyId, string awsSecretAccessKey, string keyName)
        {
            try
            {
                var client = new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, RegionEndpoint.APNortheast2);
                var res = client.GetObjectAsync(bucketName, keyName);
                res.Wait();

                if (res.Result.ResponseStream == null)
                {
                    return null;
                }

                StreamReader streamRead = new StreamReader(res.Result.ResponseStream);
                return JObject.Parse(streamRead.ReadToEnd());
            }
            catch (System.Exception e)
            {
                return null;
            }
        }

        public static bool ExistsFile(string bucketName, string awsAccessKeyId, string awsSecretAccessKey, string keyName)
        {
            try
            {
                var client = new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, RegionEndpoint.APNortheast2);
                var res = client.GetObjectAsync(bucketName, keyName);
                res.Wait();

                if (res.Result.ResponseStream == null)
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static bool ExistFileFromBucket(string bucketName, string awsAccessKeyId, string awsSecretAccessKey, string keyName)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(keyName);
                request.ContentType = "application/json";
                request.Method = "GET";
                var response = (HttpWebResponse)request.GetResponse();
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                }

                return true;
            }
            catch (WebException webExcp)
            {
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        public static bool UploadToS3(string bucketName, string awsAccessKeyId, string awsSecretAccessKey, string keyName, string sourcePath)
        {
            var client = new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, RegionEndpoint.APNortheast2);
            var res = UploadFileAsync(client, sourcePath, bucketName, keyName);
            res.Wait();
            return res.Result;
        }

        static async Task<bool> UploadFileAsync(AmazonS3Client client, string sourcePath, string bucketName, string keyName)
        {
            try
            {
                var putObjectRequest = new Amazon.S3.Model.PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                    CannedACL = S3CannedACL.PublicRead,
                    FilePath = sourcePath,
                };

                var res = await client.PutObjectAsync(putObjectRequest);
                //logger.Info($"Upload completed. keyName: {keyName}");
            }
            catch (AmazonS3Exception e)
            {
                //logger.Error($"Error encountered on server. Message:'{e.Message}' when writing an object");
                return false;
            }
            catch (Exception e)
            {
                //logger.Error($"Unknown encountered on server. Message:'{e.Message}' when writing an object");
                return false;
            }

            return true;
        }


        public static string UnderScoreToPascalCase(string text)
        {
            if (string.IsNullOrEmpty(text) || !text.Contains("_"))
            {
                return PascalCase(text);
            }
            string[] array = text.Split('_');
            for (int i = 0; i < array.Length; i++)
            {
                string s = array[i];
                string first = string.Empty;
                string rest = string.Empty;
                if (s.Length > 0)
                {
                    first = Char.ToUpperInvariant(s[0]).ToString();
                }
                if (s.Length > 1)
                {
                    rest = s.Substring(1).ToLowerInvariant();
                }
                array[i] = first + rest;
            }
            string newText = string.Join("", array);
            if (newText.Length > 0)
            {
                newText = Char.ToUpperInvariant(newText[0]) + newText.Substring(1);
            }
            else
            {
                newText = text;
            }
            return newText;
        }



        public static string PascalCase(string s)
        {
            var x = s.Replace("_", "");
            if (x.Length == 0) return "null";
            x = Regex.Replace(x, "([A-Z])([A-Z]+)($|[A-Z])",
                m => m.Groups[1].Value + m.Groups[2].Value.ToLower() + m.Groups[3].Value);
            return char.ToUpper(x[0]) + x.Substring(1);
        }


        public static string[] SplitPath(string s)
        {
            var words = s.Split(Path.AltDirectorySeparatorChar);
            if (words.Length == 1)
            {
                words = s.Split(Path.DirectorySeparatorChar);
            }

            if (words[words.Length - 1] == string.Empty)
            {
                List<string> listWord = new List<string>(words);
                listWord.RemoveAt(listWord.Count - 1);
                words = listWord.ToArray();
            }

            return words;
        }

        public static string GetNameSpace(string path)
        {
            var words = new List<string>(Helpers.SplitPath(path));
            var index = words.FindIndex(0, x => x == "Template");
            if (index == -1)
            {
                index = words.FindIndex(0, x => x == "Application");
                index += 1;
            }

            return string.Join(".", words.ToArray(), index, words.Count - index);
        }
    }
}
