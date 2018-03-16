using ServerFileSync.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace ServerFileSync.Controllers
{
    public class FileTransferController : ApiController
    {
        private string _root;
        private IFileManager _fileManager;
        private IFileNotifier _hubWrapper;

        public FileTransferController()
        {
            _root = ConfigurationManager.AppSettings["SyncFolder"].ToString();
            _fileManager =  new FileSystemFileManager(_root);
            _hubWrapper = FileSyncHubWrapper.Instance;
        }

        /// <summary>
        /// Constructor for Unit Testing
        /// </summary>
        /// <param name="fileManager"></param>
        /// <param name="hubWrapper"></param>
        /// <param name="root"></param>
        public FileTransferController(IFileManager fileManager, IFileNotifier hubWrapper, string root)
        {
            _fileManager = fileManager;
            _hubWrapper = hubWrapper;
            _root = root;
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Upload()
        {
            HttpRequestMessage request = this.Request;
            if (!request.Content.IsMimeMultipartContent())
            {
                return new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType);
                //throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var multiContents = await request.Content.ReadAsMultipartAsync();

            try
            {
                string fileName = await GetFileNameFromRequest(multiContents);

                byte[] fileBytes = await GetFileBytesFromRequest(multiContents);

                _fileManager.Save(fileName, fileBytes);

                //Calculate CRC *************************************************************************** TO DO
                string CRC = "";

                //Notify New File            
                //hub.NotifyNewFile(provider.GetOriginalFileName, CRC);
                _hubWrapper.NotifyNewFile(fileName, CRC);

                //return task;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (HttpResponseException)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
            catch (Exception)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
        
        public HttpResponseMessage Delete(string filename, string extension)
        {
            try
            {
                if (String.IsNullOrEmpty(filename) || extension == null)
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                _fileManager.Delete(filename + "." + extension);
                _hubWrapper.NotifyDeleteFile(filename);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (IOException excp)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Problems while deleting file. " + excp.Message) };                
            }
            catch (Exception excp)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(excp.Message) };                
            }
        }

        private async Task<byte[]> GetFileBytesFromRequest(MultipartMemoryStreamProvider multiContents)
        {
            byte[] fileBytes;
            if (multiContents.Contents.Count > 1)
            {
                //The file as byte array
                fileBytes = await multiContents.Contents[1].ReadAsByteArrayAsync();
                if (fileBytes.Length <= 0)
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            return fileBytes;
        }

        private async Task<string> GetFileNameFromRequest(MultipartMemoryStreamProvider multiContents)
        {
            string fileName;
            if (multiContents.Contents.Count > 0)
            {
                //Filename as string content
                fileName = await multiContents.Contents[0].ReadAsStringAsync();
                if (String.IsNullOrEmpty(fileName))
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            return fileName;
        }

        [HttpGet]
        public HttpResponseMessage Exists(string fileName)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (String.IsNullOrEmpty(fileName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }
            else
            {
                response.Content = new StringContent(_fileManager.Exists(fileName) ? "true" : "false");                
                return response;
            }
            //return File.Exists(_root + "\\" + fileName);
        }

        [HttpGet]
        public HttpResponseMessage Download(string fileName)
        {
            //FileStream sourceStream = File.Open(_root + "\\" + fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            FileStream sourceStream = _fileManager.GetStream(fileName);

            HttpResponseMessage fullResponse = Request.CreateResponse(HttpStatusCode.OK);
            fullResponse.Content = new StreamContent(sourceStream);

            fullResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return fullResponse;
        }
    }
}
