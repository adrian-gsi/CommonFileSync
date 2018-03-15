using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using ServerFileSync;
using ServerFileSync.Controllers;
using ServerFileSync.Interfaces;

namespace UnitTestServerFileSync
{
    [TestClass]
    public class FileTransferControllerTest
    {
        FileTransferController fileController;
        Mock<IFileManager> mockFileManager;
        Mock<IFileNotifier> mockHubWrapper;

        [TestInitialize]
        public void Startup()
        {
            mockFileManager = new Mock<IFileManager>();
            //mockFileManager.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            var mockFileManagerOject = mockFileManager.Object;


            mockHubWrapper = new Mock<IFileNotifier>();
            var mockHubWrapperObject = mockHubWrapper.Object;

            fileController = new FileTransferController(mockFileManagerOject, mockHubWrapperObject, "c:\\GSI");// (mockFileManager.Object, mockHubWrapper.Object);

   
        }

        [TestMethod]
        public void Upload_NoFileName_ReturnsBadRequest()
        {
            //ARRANGE

            byte[] fileBytes = new byte[] { 1, 2, 3 };
            HttpContent fileContent = new ByteArrayContent(fileBytes);
            var fileName = "MyFile.txt";

            fileController.Request = new System.Net.Http.HttpRequestMessage();
            fileController.Request.Content = new MultipartFormDataContent()
                                                        {
                                                            //{ new StringContent(fileName),fileName},
                                                            { fileContent, "file", fileName }
                                                        };


            //ACT
            var result = fileController.Upload().Result;

            //ASSERT
            mockFileManager.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
            mockHubWrapper.Verify(x => x.NotifyNewFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.AreEqual(result.StatusCode, System.Net.HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public void Upload_NoFile_ReturnsBadRequest()
        {
            //ARRANGE            
            var fileName = "MyFile.txt";

            fileController.Request = new System.Net.Http.HttpRequestMessage();
            fileController.Request.Content = new MultipartFormDataContent()
                                                        {
                                                            { new StringContent(fileName),fileName},
                                                            //{ fileContent, "file", fileName }
                                                        };


            //ACT
            var result = fileController.Upload().Result;

            //ASSERT
            mockFileManager.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
            mockHubWrapper.Verify(x => x.NotifyNewFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.AreEqual(result.StatusCode, System.Net.HttpStatusCode.BadRequest);
        }
        
        [TestMethod]        
        public void Upload_ParamsOK_SaveNotifyAndReturnsOK()
        {
            //ARRANGE
            byte[] fileBytes = new byte[] { 1, 2, 3 };
            HttpContent fileContent = new ByteArrayContent(fileBytes);
            var fileName = "MyFile.txt";

            fileController.Request = new System.Net.Http.HttpRequestMessage();
            fileController.Request.Content = new MultipartFormDataContent()
                                                        {
                                                            { new StringContent(fileName),fileName},
                                                            { fileContent, "file", fileName }
                                                        };


            //ACT
            var result = fileController.Upload().Result;

            //ASSERT
            mockFileManager.Verify(x => x.Save(fileName, fileBytes), Times.Once);
            mockHubWrapper.Verify(x => x.NotifyNewFile(fileName, It.IsAny<string>()), Times.Once);
            Assert.AreEqual(result.StatusCode, System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public void Exists_EmptyParam_ReturnsBadRequest()
        {
            //ARRANGE
            byte[] fileBytes = new byte[] { 1, 2, 3 };
            HttpContent fileContent = new ByteArrayContent(fileBytes);
            var fileName = "MyFile.txt";

            fileController.Request = new System.Net.Http.HttpRequestMessage();
            fileController.Request.Content = new MultipartFormDataContent()
                                                        {
                                                            { new StringContent(fileName),fileName},
                                                            { fileContent, "file", fileName }
                                                        };


            //ACT
            var result = fileController.Exists("");

            //ASSERT
            mockFileManager.Verify(x => x.Exists(It.IsAny<string>()), Times.Never);
            Assert.AreEqual(result.StatusCode, System.Net.HttpStatusCode.BadRequest);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Exists_ParamsOK_ReturnsFileManagerExistsMethod(bool existsMethodResult)
        {
            //ARRANGE
            mockFileManager.Setup(x => x.Exists(It.IsAny<string>())).Returns(existsMethodResult);
            byte[] fileBytes = new byte[] { 1, 2, 3 };
            HttpContent fileContent = new ByteArrayContent(fileBytes);
            var fileName = "MyFile.txt";

            fileController.Request = new System.Net.Http.HttpRequestMessage();
            fileController.Request.Content = new MultipartFormDataContent()
                                                        {
                                                            { new StringContent(fileName),fileName},
                                                            { fileContent, "file", fileName }
                                                        };


            //ACT
            var result = fileController.Exists(fileName);

            //ASSERT
            mockFileManager.Verify(x => x.Exists(fileName), Times.Once);
            Assert.AreEqual(result.StatusCode, System.Net.HttpStatusCode.OK);
            Assert.AreEqual(existsMethodResult, JsonConvert.DeserializeObject<bool>(result.Content.ReadAsStringAsync().Result));
        }
    }
}
