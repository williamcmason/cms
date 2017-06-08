﻿#region Copyright
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2014
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using DotNetNuke.Common.Utilities;
using DotNetNuke.ComponentModel;
using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Entities.Content.Data;
using DotNetNuke.Services.Cache;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Tests.Content.Mocks;
using DotNetNuke.Tests.Utilities;
using DotNetNuke.Tests.Utilities.Mocks;
using Moq;

using NUnit.Framework;
using FileController = DotNetNuke.Entities.Content.AttachmentController;
using Util = DotNetNuke.Entities.Content.Common.Util;

namespace DotNetNuke.Tests.Content
{
    [TestFixture]
    public class AttachmentControllerTests
    {
        [SetUp]
        public void SetUp()
        {
            MockComponentProvider.CreateDataProvider().Setup(c => c.GetProviderPath()).Returns(string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            MockComponentProvider.ResetContainer();
        }

        #region Utility methods

        private static Mock<IDataService> DataServiceFactory()
        {
            var dataService = new Mock<IDataService>();

            dataService.Setup(ds =>
             ds.SynchronizeMetaData(
                 It.IsAny<ContentItem>(),
                 It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
                 It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
             .Callback<ContentItem, IEnumerable<KeyValuePair<string, string>>, IEnumerable<KeyValuePair<string, string>>>(
                 (ci, added, deleted) =>
                 {
                     deleted.ToList().ForEach(
                         item => dataService.Object.DeleteMetaData(ci, item.Key, item.Value));

                     added.ToList().ForEach(
                         item => dataService.Object.AddMetaData(ci, item.Key, item.Value));
                 });

            // Register controller types that are dependent on our IDataService.
            var contentController = new ContentController(dataService.Object);

            ComponentFactory.RegisterComponentInstance<IAttachmentController>(new FileController(contentController));
            ComponentFactory.RegisterComponentInstance<IContentController>(contentController);
            ComponentFactory.RegisterComponentInstance<IFileManager>(MockHelper.CreateMockFileManager().Object);

            return dataService;
        }

        private static Mock<IDataService> DataServiceFactoryWithLocalMetaData(ref Dictionary<string, string> metadata)
        {
            var dataService = DataServiceFactory();

            var closure = metadata;

            dataService.Setup(ds => ds.GetMetaData(It.IsAny<int>())).Returns(() => MockHelper.CreateMetaDataReaderFromDictionary(closure));
            dataService.Setup(ds => ds.AddMetaData(It.IsAny<ContentItem>(), It.IsAny<string>(), It.IsAny<string>())).
                Callback<ContentItem, string, string>((ci, name, value) => closure[name] = value);
            dataService.Setup(ds => ds.DeleteMetaData(It.IsAny<ContentItem>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<ContentItem, string, string>((ci, key, val) => closure.Remove(key));

            return dataService;
        }

        #endregion

        #region FileController Tests

        [Test]
        public void Test_Add_File_To_Content_Item_Without_Metadata()
        {
            var dataService = DataServiceFactory();

            dataService.Setup(ds => ds.AddContentItem(It.IsAny<ContentItem>(), It.IsAny<int>()))
                .Returns(Constants.CONTENT_AddContentItemId);

            // Return empty set of metadata.
            dataService.Setup(ds => ds.GetMetaData(It.IsAny<int>())).Returns(MockHelper.CreateEmptyMetaDataReader);
            
            var content = ContentTestHelper.CreateValidContentItem();
            content.Metadata.Clear();

            var contentId = Util.GetContentController().AddContentItem(content);
            Assert.AreEqual(contentId, Constants.CONTENT_AddContentItemId);
            Assert.IsEmpty(content.Metadata);

            dataService.Setup(ds => ds.GetContentItem(It.IsAny<int>()))
                .Returns<int>(y => MockHelper.CreateValidContentItemReader(content));

            var fileController = ComponentFactory.GetComponent<IAttachmentController>();

            fileController.AddFileToContent(contentId, ContentTestHelper.CreateValidFile(0));

            dataService.Verify(
                ds => ds.AddMetaData(It.IsAny<ContentItem>(), FileController.FilesKey, new[] {0}.ToJson()));
        }

        [Test]
        public void Test_Load_Attachments_From_DataService()
        {
            var files = new List<IFileInfo>
                {
                    ContentTestHelper.CreateValidFile(0),
                    ContentTestHelper.CreateValidFile(1),
                    ContentTestHelper.CreateValidFile(2)
                };

            var dataService = DataServiceFactory();

            dataService.Setup(ds => ds.GetContentItem(It.IsAny<int>())).Returns(MockHelper.CreateValidContentItemReader);

            dataService.Setup(
                ds =>
                    ds.GetMetaData(It.IsAny<int>())).Returns(
                        () => MockHelper.CreateMetaDataReaderWithFiles(files, new IFileInfo[0], new IFileInfo[0]));
            
            var contentItem = Util.GetContentController().GetContentItem(Constants.CONTENT_ValidContentItemId);
            Assert.IsNotNull(contentItem);

            var serialized = contentItem.Metadata[FileController.FilesKey];
            Assert.IsNotEmpty(serialized);

            Assert.IsNotEmpty(contentItem.Files);
            Assert.AreEqual(contentItem.Files.Count, 3);
            Assert.AreEqual(contentItem.Files[0].FileId, 0);
            Assert.AreEqual(contentItem.Files[1].FileId, 1);
            Assert.AreEqual(contentItem.Files[2].FileId, 2);
        }

        [Test]
        public void Test_Add_Attachments_With_FileController()
        {
            var dataService = DataServiceFactory();

            dataService.Setup(
                ds =>
                    ds.GetContentItem(It.IsAny<int>())).Returns(MockHelper.CreateValidContentItemReader);

            // Use a closure to store the metadata locally in this method.
            var data = new Dictionary<string, string>();
            
            dataService.Setup(
                ds =>
                    ds.GetMetaData(It.IsAny<int>())).Returns(
                        () => MockHelper.CreateMetaDataReaderFromDictionary(data));

            dataService.Setup(
                    ds =>
                        ds.AddMetaData(
                            It.IsAny<ContentItem>(),
                            It.IsAny<string>(),
                            It.IsAny<string>()))
                .Callback<ContentItem, string, string>((ci, name, value) => data[name] = value);

            var contentController = Util.GetContentController();

            var contentItem = contentController.GetContentItem(Constants.CONTENT_ValidContentItemId);
            Assert.IsNotNull(contentItem);

            var serialized = contentItem.Metadata[FileController.FilesKey];
            Assert.IsNull(serialized);
            Assert.IsEmpty(contentItem.Files);

            var fileManager = ComponentFactory.GetComponent<IFileManager>();

            // Add some files.
            var fileController = ComponentFactory.GetComponent<IAttachmentController>();
            fileController.AddFileToContent(contentItem.ContentItemId, fileManager.GetFile(0));
            fileController.AddFileToContent(contentItem.ContentItemId, fileManager.GetFile(1));

            contentItem = contentController.GetContentItem(Constants.CONTENT_ValidContentItemId);

            Assert.AreEqual(contentItem.Files.Count, 2);
            Assert.AreEqual(contentItem.Files[0].FileId, 0);
            Assert.AreEqual(contentItem.Files[1].FileId, 1);

            dataService.Verify(
                ds => ds.DeleteMetaData(It.IsAny<ContentItem>(), FileController.FilesKey, "[0]"), Times.Once());

            dataService.Verify(
                ds => ds.AddMetaData(It.IsAny<ContentItem>(), FileController.FilesKey, "[0]"), Times.Once());
            
            dataService.Verify(
                ds => ds.AddMetaData(It.IsAny<ContentItem>(), FileController.FilesKey, "[0,1]"), Times.Once());
        }

        [Test]
        public void Set_MetaData_To_Empty_Value_Deletes_Row()
        {
            var data = new Dictionary<string, string>();

            var dataService = DataServiceFactoryWithLocalMetaData(ref data);

            dataService.Setup(ds => ds.GetContentItem(It.IsAny<int>())).Returns(MockHelper.CreateValidContentItemReader);

            var contentController = Util.GetContentController();

            var fileManager = ComponentFactory.GetComponent<IFileManager>();

            var contentItem = contentController.GetContentItem(Constants.CONTENT_ValidContentItemId);
            Assert.IsNotNull(contentItem);

            var serialized = contentItem.Metadata[FileController.FilesKey];
            Assert.IsNull(serialized);
            Assert.IsEmpty(contentItem.Files);

            // Add some files.
            var fileController = ComponentFactory.GetComponent<IAttachmentController>();
            fileController.AddFileToContent(contentItem.ContentItemId, fileManager.GetFile(0));
            fileController.AddFileToContent(contentItem.ContentItemId, fileManager.GetFile(1));

            contentItem = contentController.GetContentItem(Constants.CONTENT_ValidContentItemId);

            Assert.AreEqual(contentItem.Files.Count, 2);
            Assert.AreEqual(contentItem.Files[0].FileId, 0);
            Assert.AreEqual(contentItem.Files[1].FileId, 1);
            Assert.IsNotEmpty(contentItem.Metadata[FileController.FilesKey]);

            contentItem.Files.Clear();

            contentController.UpdateContentItem(contentItem);

            dataService.Verify(ds => ds.DeleteMetaData(It.IsAny<ContentItem>(), FileController.FilesKey, "[0]"), Times.Once());
            dataService.Verify(ds => ds.DeleteMetaData(It.IsAny<ContentItem>(), FileController.FilesKey, "[0,1]"), Times.Once());
            dataService.Verify(ds => ds.AddMetaData(It.IsAny<ContentItem>(), FileController.FilesKey, "[0]"), Times.Once());
            dataService.Verify(ds => ds.AddMetaData(It.IsAny<ContentItem>(), FileController.FilesKey, "[0,1]"), Times.Once());

            var emptyFiles = fileController.GetFilesByContent(contentItem.ContentItemId);
            Assert.AreEqual(0, emptyFiles.Count);
        }

        /// <remarks>This test should be moved elsewhere (cb)</remarks>
        [Test]
        public void Set_MetaData_To_Same_Value_Doesnt_Update_Database_Entry()
        {
            var metadata = new Dictionary<string, string>();

            var dataService = DataServiceFactoryWithLocalMetaData(ref metadata);

            var contentController = Util.GetContentController();

            var contentItem = ContentTestHelper.CreateValidContentItem();

            contentController.AddContentItem(contentItem);

            dataService.Verify(ds => ds.AddMetaData(contentItem, FileController.TitleKey, It.IsAny<string>()), Times.Never());
            dataService.Verify(ds => ds.DeleteMetaData(contentItem, FileController.TitleKey, It.IsAny<string>()), Times.Never());

            contentItem.ContentTitle = "Foobar";
            
            contentController.UpdateContentItem(contentItem);

            dataService.Verify(ds => ds.AddMetaData(contentItem, FileController.TitleKey, It.IsAny<string>()), Times.Once());
            dataService.Verify(ds => ds.DeleteMetaData(contentItem, FileController.TitleKey, It.IsAny<string>()), Times.Never());

            contentItem.ContentTitle = "Foobar";

            contentController.UpdateContentItem(contentItem);

            // Should be a no-op since no real data changed
            dataService.Verify(ds => ds.AddMetaData(contentItem, FileController.TitleKey, It.IsAny<string>()), Times.Once());
            dataService.Verify(ds => ds.DeleteMetaData(contentItem, FileController.TitleKey, It.IsAny<string>()), Times.Never());


            // Really update
            contentItem.ContentTitle = "SNAFU";

            contentController.UpdateContentItem(contentItem);

            dataService.Verify(ds => ds.AddMetaData(contentItem, FileController.TitleKey, It.IsAny<string>()), Times.Exactly(2));
            dataService.Verify(ds => ds.DeleteMetaData(contentItem, FileController.TitleKey, It.IsAny<string>()), Times.Once());
        }
        
        #endregion
    }
}