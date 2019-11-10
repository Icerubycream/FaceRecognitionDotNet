using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using DlibDotNet;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FaceRecognitionDotNet.Tests
{

    [TestClass]
    public class FaceRecognitionTest
    {

        #region Fields 

        private FaceRecognition _FaceRecognition;

        private const string ImageDirectory = "Images";

        private const string ModelDirectory = "Models";

        private const string ModelTempDirectory = "TempModels";

        private readonly IList<string> ModelFiles = new List<string>();

        private const string ModelBaseUrl = "https://github.com/ageitgey/face_recognition_models/raw/master/face_recognition_models/models";

        private const string ResultDirectory = "Result";

        private const string TwoPersonUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/Official_portrait_of_President_Obama_and_Vice_President_Biden_2012.jpg";

        private const string TwoPersonFile = "419px-Official_portrait_of_President_Obama_and_Vice_President_Biden_2012.jpg";

        private bool _HasHelenDataset = false;

        private bool _HasAgeDataset = false;

        private bool _HasGenderDataset = false;

        #endregion

        #region Methods

        [TestCleanup]
        public void Cleanup()
        {
            var array = this.ModelFiles.ToArray();

            // Remove all files 
            foreach (var file in array)
            {
                var path = Path.Combine(ModelTempDirectory, file);
                if (File.Exists(path))
                    File.Delete(path);
            }

            if (Directory.Exists(ModelTempDirectory))
                Directory.Delete(ModelTempDirectory);

            this._FaceRecognition?.Dispose();
        }

        [TestMethod]
        public void CompareFacesFalse()
        {
            var bidenUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/64/Biden_2013.jpg";
            var bidenFile = "480px-Biden_2013.jpg";
            var obamaUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8d/President_Barack_Obama.jpg";
            var obamaFile = "480px-President_Barack_Obama.jpg";

            var path1 = Path.Combine(ImageDirectory, bidenFile);
            if (!File.Exists(path1))
            {
                var url = $"{bidenUrl}/{bidenFile}";
                var binary = new HttpClient().GetByteArrayAsync(url).Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path1, binary);
            }

            var path2 = Path.Combine(ImageDirectory, obamaFile);
            if (!File.Exists(path2))
            {
                var url = $"{obamaUrl}/{obamaFile}";
                var binary = new HttpClient().GetByteArrayAsync(url).Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path2, binary);
            }

            bool atLeast1Time = false;

            using (var image1 = FaceRecognition.LoadImageFile(path1))
            using (var image2 = FaceRecognition.LoadImageFile(path2))
            {
                foreach (var numJitters in new[] { 1, 2 })
                    foreach (var model in Enum.GetValues(typeof(PredictorModel)).Cast<PredictorModel>())
                    {
                        try
                        {
                            var encodings1 = this._FaceRecognition.FaceEncodings(image1, null, numJitters, model).ToArray();
                            var encodings2 = this._FaceRecognition.FaceEncodings(image2, null, numJitters, model).ToArray();

                            foreach (var encoding in encodings1)
                            foreach (var compareFace in FaceRecognition.CompareFaces(encodings2, encoding))
                            {
                                atLeast1Time = true;
                                Assert.IsFalse(compareFace, $"{nameof(numJitters)}: {numJitters}");
                            }

                            foreach (var encoding in encodings1)
                                encoding.Dispose();
                            foreach (var encoding in encodings2)
                                encoding.Dispose();
                        }
                        catch (ArgumentException)
                        {
                            if (model != PredictorModel.Helen)
                                throw;
                        }
                    }
            }

            if (!atLeast1Time)
                Assert.Fail("Assert check did not execute");
        }

        [TestMethod]
        public void CompareFacesTrue()
        {
            var obamaUrl1 = "https://upload.wikimedia.org/wikipedia/commons/3/3f";
            var obamaFile1 = "Barack_Obama_addresses_LULAC_7-8-08.JPG";
            var obamaUrl2 = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8d/President_Barack_Obama.jpg";
            var obamaFile2 = "480px-President_Barack_Obama.jpg";

            var path1 = Path.Combine(ImageDirectory, obamaFile1);
            if (!File.Exists(path1))
            {
                var url = $"{obamaUrl1}/{obamaFile1}";
                var binary = new HttpClient().GetByteArrayAsync(url).Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path1, binary);
            }

            var path2 = Path.Combine(ImageDirectory, obamaFile2);
            if (!File.Exists(path2))
            {
                var url = $"{obamaUrl2}/{obamaFile2}";
                var binary = new HttpClient().GetByteArrayAsync(url).Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path2, binary);
            }

            bool atLeast1Time = false;

            using (var image1 = FaceRecognition.LoadImageFile(path1))
            using (var image2 = FaceRecognition.LoadImageFile(path2))
            {
                foreach (var numJitters in new[] { 1, 2 })
                    foreach (var model in Enum.GetValues(typeof(PredictorModel)).Cast<PredictorModel>())
                    {
                        try
                        {
                            var endodings1 = this._FaceRecognition.FaceEncodings(image1, null, numJitters, model).ToArray();
                            var endodings2 = this._FaceRecognition.FaceEncodings(image2, null, numJitters, model).ToArray();

                            foreach (var encoding in endodings1)
                            foreach (var compareFace in FaceRecognition.CompareFaces(endodings2, encoding))
                            {
                                atLeast1Time = true;
                                Assert.IsTrue(compareFace, $"{nameof(numJitters)}: {numJitters}");
                            }

                            foreach (var encoding in endodings1)
                                encoding.Dispose();
                            foreach (var encoding in endodings2)
                                encoding.Dispose();
                        }
                        catch (ArgumentException)
                        {
                            if (model != PredictorModel.Helen)
                                throw;
                        }
                    }
            }

            if (!atLeast1Time)
                Assert.Fail("Assert check did not execute");
        }

        [TestMethod]
        public void CreateFail1()
        {
            Directory.CreateDirectory(ModelTempDirectory);

            var array = this.ModelFiles.ToArray();
            for (var j = 0; j < array.Length; j++)
            {
                // Remove all files 
                foreach (var file in array)
                {
                    var path = Path.Combine(ModelTempDirectory, file);
                    if (File.Exists(path))
                        File.Delete(path);
                }

                for (var i = 0; i < array.Length; i++)
                {
                    if (i == j)
                        continue;

                    File.Copy(Path.Combine(ModelDirectory, array[i]), Path.Combine(ModelTempDirectory, array[i]));
                }

                FaceRecognition faceRecognition = null;
                try
                {
                    faceRecognition = FaceRecognition.Create(ModelTempDirectory);
                    Assert.Fail($"{Path.Combine(ModelTempDirectory, array[j])} is missing and Create method should throw exception.");
                }
                catch (FileNotFoundException)
                {
                }
                finally
                {
                    faceRecognition?.Dispose();
                }
            }
        }

        [TestMethod]
        public void CreateFail2()
        {
            var tempModelDirectory = "Temp";
            FaceRecognition faceRecognition = null;

            try
            {
                faceRecognition = FaceRecognition.Create(tempModelDirectory);
                Assert.Fail($"{tempModelDirectory} directory is missing and Create method should throw exception.");
            }
            catch (DirectoryNotFoundException)
            {
            }
            finally
            {
                faceRecognition?.Dispose();
            }
        }

        [TestMethod]
        public void FaceDistance()
        {
            var bidenUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/64/Biden_2013.jpg";
            var bidenFile = "480px-Biden_2013.jpg";
            var obamaUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8d/President_Barack_Obama.jpg";
            var obamaFile = "480px-President_Barack_Obama.jpg";

            var path1 = Path.Combine(ImageDirectory, bidenFile);
            if (!File.Exists(path1))
            {
                var url = $"{bidenUrl}/{bidenFile}";
                var binary = new HttpClient().GetByteArrayAsync(url).Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path1, binary);
            }

            var path2 = Path.Combine(ImageDirectory, obamaFile);
            if (!File.Exists(path2))
            {
                var url = $"{obamaUrl}/{obamaFile}";
                var binary = new HttpClient().GetByteArrayAsync(url).Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path2, binary);
            }

            bool atLeast1Time = false;

            foreach (var mode in new[] { Mode.Rgb, Mode.Greyscale })
            {
                using (var image1 = FaceRecognition.LoadImageFile(path1, mode))
                using (var image2 = FaceRecognition.LoadImageFile(path2, mode))
                {
                    var endodings1 = this._FaceRecognition.FaceEncodings(image1).ToArray();
                    var endodings2 = this._FaceRecognition.FaceEncodings(image2).ToArray();
                    Assert.IsTrue(endodings1.Length >= 1, $"{nameof(endodings1)} has {endodings1.Length} faces");
                    Assert.IsTrue(endodings2.Length >= 1, $"{nameof(endodings2)} has {endodings2.Length} faces");

                    foreach (var e1 in endodings1)
                        foreach (var e2 in endodings2)
                        {
                            atLeast1Time = true;
                            var distance = FaceRecognition.FaceDistance(e1, e2);
                            Assert.IsTrue(distance > 0.6d);
                        }

                    foreach (var encoding in endodings1)
                        encoding.Dispose();
                    foreach (var encoding in endodings2)
                        encoding.Dispose();
                }
            }

            if (!atLeast1Time)
                Assert.Fail("Assert check did not execute");
        }

        [TestMethod]
        public void FaceDistanceDeserialized()
        {
            var bidenUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/64/Biden_2013.jpg";
            var bidenFile = "480px-Biden_2013.jpg";
            var obamaUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8d/President_Barack_Obama.jpg";
            var obamaFile = "480px-President_Barack_Obama.jpg";

            var path1 = Path.Combine(ImageDirectory, bidenFile);
            if (!File.Exists(path1))
            {
                var url = $"{bidenUrl}/{bidenFile}";
                var binary = new HttpClient().GetByteArrayAsync(url).Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path1, binary);
            }

            var path2 = Path.Combine(ImageDirectory, obamaFile);
            if (!File.Exists(path2))
            {
                var url = $"{obamaUrl}/{obamaFile}";
                var binary = new HttpClient().GetByteArrayAsync(url).Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path2, binary);
            }

            bool atLeast1Time = false;

            foreach (var mode in new[] { Mode.Rgb, Mode.Greyscale })
            {
                using (var image1 = FaceRecognition.LoadImageFile(path1, mode))
                using (var image2 = FaceRecognition.LoadImageFile(path2, mode))
                {
                    var endodings1 = this._FaceRecognition.FaceEncodings(image1).ToArray();
                    var endodings2 = this._FaceRecognition.FaceEncodings(image2).ToArray();
                    Assert.IsTrue(endodings1.Length >= 1, $"{nameof(endodings1)} has {endodings1.Length} faces");
                    Assert.IsTrue(endodings2.Length >= 1, $"{nameof(endodings2)} has {endodings2.Length} faces");

                    foreach (var e1 in endodings1)
                        foreach (var e2 in endodings2)
                        {
                            atLeast1Time = true;
                            var distance = FaceRecognition.FaceDistance(e1, e2);
                            Assert.IsTrue(distance > 0.6d, $"distance should be greater than 0.6 but {distance}.");

                            var bf = new BinaryFormatter();
                            using (var ms1 = new MemoryStream())
                            {
                                bf.Serialize(ms1, e1);
                                ms1.Flush();

                                var array = ms1.ToArray();
                                using (var ms2 = new MemoryStream(array))
                                {
                                    var de1 = bf.Deserialize(ms2) as FaceEncoding;
                                    var distance2 = FaceRecognition.FaceDistance(de1, e2);
                                    Assert.IsTrue(Math.Abs(distance - distance2) < double.Epsilon);
                                }
                            }
                        }

                    foreach (var encoding in endodings1)
                        encoding.Dispose();
                    foreach (var encoding in endodings2)
                        encoding.Dispose();
                }
            }

            if (!atLeast1Time)
                Assert.Fail("Assert check did not execute");
        }

        [TestMethod]
        public void FaceEncodings()
        {
            var path = Path.Combine(ImageDirectory, TwoPersonFile);
            if (!File.Exists(path))
            {
                var binary = new HttpClient().GetByteArrayAsync($"{TwoPersonUrl}/{TwoPersonFile}").Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path, binary);
            }

            foreach (var mode in new[] { Mode.Rgb, Mode.Greyscale })
            {
                using (var image = FaceRecognition.LoadImageFile(path, mode))
                {
                    var encodings = this._FaceRecognition.FaceEncodings(image).ToArray();
                    Assert.IsTrue(encodings.Length > 1, "");

                    foreach (var encoding in encodings)
                    {
                        encoding.Dispose();

                        try
                        {
                            encoding.Dispose();
                        }
                        catch
                        {
                            Assert.Fail($"{typeof(FaceEncoding)} must not throw exception even though {nameof(FaceEncoding.Dispose)} method is called again.");
                        }

                        try
                        {
                            var _ = encoding.Size;
                            Assert.Fail($"{nameof(FaceEncoding.Size)} must throw {typeof(ObjectDisposedException)} after object is disposed.");
                        }
                        catch
                        {
                        }
                    }

                    foreach (var encoding in encodings)
                        Assert.IsTrue(encoding.IsDisposed, $"{typeof(FaceEncoding)} should be already disposed.");
                }
            }
        }

        [TestMethod]
        public void FaceEncodingsException()
        {
            try
            {
                var _ = this._FaceRecognition.FaceEncodings(null).ToArray();
                Assert.Fail($"{nameof(FaceRecognition.FaceEncodings)} must throw {typeof(ArgumentNullException)}.");
            }
            catch (ArgumentNullException)
            {
            }
        }

        [TestMethod]
        public void FaceLandmarkLarge()
        {
            const string testName = nameof(this.FaceLandmarkLarge);
            this.FaceLandmark(testName, PredictorModel.Large, true);
            this.FaceLandmark(testName, PredictorModel.Large, false);
        }

        [TestMethod]
        public void FaceLandmarkSmall()
        {
            const string testName = nameof(this.FaceLandmarkSmall);
            this.FaceLandmark(testName, PredictorModel.Small, true);
            this.FaceLandmark(testName, PredictorModel.Small, false);
        }

        [TestMethod]
        public void FaceLandmarkException()
        {
            try
            {
                var _ = this._FaceRecognition.FaceLandmark(null).ToArray();
                Assert.Fail($"{nameof(FaceRecognition.FaceLandmark)} must throw {typeof(ArgumentNullException)}.");
            }
            catch (ArgumentNullException)
            {
            }
        }

        [TestMethod]
        public void FaceLandmarkHelen()
        {
            if (this._HasHelenDataset)
            {
                const string testName = nameof(this.FaceLandmarkHelen);
                this.FaceLandmark(testName, PredictorModel.Helen, true);
                this.FaceLandmark(testName, PredictorModel.Helen, false);
            }
        }

        [TestMethod]
        public void FaceLocationCnn()
        {
            const string testName = nameof(this.FaceLocationCnn);
            this.FaceLocation(testName, 2, Model.Cnn);
            this.FaceLocation(testName, 1, Model.Cnn);
            this.FaceLocation(testName, 0, Model.Cnn);
        }

        [TestMethod]
        public void FaceLocationHog()
        {
            const string testName = nameof(this.FaceLocationHog);
            this.FaceLocation(testName, 2, Model.Hog);
            this.FaceLocation(testName, 1, Model.Hog);
            this.FaceLocation(testName, 0, Model.Hog);
        }

        [TestMethod]
        public void FaceLocationsException()
        {
            try
            {
                var _ = this._FaceRecognition.FaceLocations(null).ToArray();
                Assert.Fail($"{nameof(FaceRecognition.FaceLocations)} must throw {typeof(ArgumentNullException)}.");
            }
            catch (ArgumentNullException)
            {
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            var faceRecognition = typeof(FaceRecognition);
            var type = faceRecognition.Assembly.GetTypes().FirstOrDefault(t => t.Name == "FaceRecognitionModels");
            if (type == null)
                Assert.Fail("FaceRecognition.FaceRecognitionModels is not found.");

            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);

            var models = new List<string>();
            foreach (var method in methods)
            {
                var result = method.Invoke(null, BindingFlags.Public | BindingFlags.Static, null, null, null) as string;
                if (string.IsNullOrWhiteSpace(result))
                    Assert.Fail($"{method.Name} does not return {typeof(string).FullName} value or return null or whitespace value.");

                switch (method.Name)
                {
                    case "GetPosePredictor194PointModelLocation":
                        this._HasHelenDataset = File.Exists(Path.Combine(ModelDirectory, result));
                        break;
                    case "GetGenderNetworkModelLocation":
                        this._HasGenderDataset = File.Exists(Path.Combine(ModelDirectory, result));
                        break;
                    case "GetAgeNetworkModelLocation":
                        this._HasAgeDataset = File.Exists(Path.Combine(ModelDirectory, result));
                        break;
                    default:
                        models.Add(result);

                        var path = Path.Combine(ModelDirectory, result);
                        if (File.Exists(path))
                            continue;

                        var binary = new HttpClient().GetByteArrayAsync($"{ModelBaseUrl}/{result}").Result;
                        Directory.CreateDirectory(ModelDirectory);
                        File.WriteAllBytes(path, binary);
                        break;
                }
            }

            foreach (var model in models)
                this.ModelFiles.Add(model);

            this._FaceRecognition = FaceRecognition.Create(ModelDirectory);
        }

        [TestMethod]
        public void LoadFaceEncoding()
        {
            var bidenUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/64/Biden_2013.jpg";
            var bidenFile = "480px-Biden_2013.jpg";

            var path1 = Path.Combine(ImageDirectory, bidenFile);
            if (!File.Exists(path1))
            {
                var url = $"{bidenUrl}/{bidenFile}";
                var binary = new HttpClient().GetByteArrayAsync(url).Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path1, binary);
            }

            bool atLeast1Time = false;

            var getMatrix = typeof(FaceEncoding).GetField("_Encoding", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var mode in new[] { Mode.Rgb, Mode.Greyscale })
            {
                using (var image1 = FaceRecognition.LoadImageFile(path1, mode))
                {
                    var encodings = this._FaceRecognition.FaceEncodings(image1).ToArray();
                    foreach (var e1 in encodings)
                    {
                        atLeast1Time = true;

                        var matrix = getMatrix.GetValue(e1) as Matrix<double>;
                        Assert.IsNotNull(matrix);

                        var fe = matrix.ToArray();

                        using (var e2 = FaceRecognition.LoadFaceEncoding(fe))
                        {
                            var distance = FaceRecognition.FaceDistance(e1, e2);
                            Console.WriteLine($"Original: {distance}");
                            Assert.IsTrue(Math.Abs(distance) < double.Epsilon);
                        }

                        fe[0] = 1;
                        using (var e2 = FaceRecognition.LoadFaceEncoding(fe))
                        {
                            var distance = FaceRecognition.FaceDistance(e1, e2);
                            Console.WriteLine($"Modified: {distance}");
                            Assert.IsTrue(Math.Abs(distance) > double.Epsilon);
                        }
                    }

                    foreach (var encoding in encodings)
                        encoding.Dispose();
                }
            }

            if (!atLeast1Time)
                Assert.Fail("Assert check did not execute");
        }

        [TestMethod]
        public void LoadFaceEncodingFail()
        {
            try
            {
                FaceRecognition.LoadFaceEncoding(null);
                Assert.Fail($"{nameof(this.FaceEncodings)}.{nameof(FaceRecognition.LoadFaceEncoding)} should throw {nameof(ArgumentNullException)}");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                FaceRecognition.LoadFaceEncoding(new double[129]);
                Assert.Fail($"{nameof(this.FaceEncodings)}.{nameof(FaceRecognition.LoadFaceEncoding)} should throw {nameof(ArgumentOutOfRangeException)}");
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                FaceRecognition.LoadFaceEncoding(new double[127]);
                Assert.Fail($"{nameof(this.FaceEncodings)}.{nameof(FaceRecognition.LoadFaceEncoding)} should throw {nameof(ArgumentOutOfRangeException)}");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        [TestMethod]
        public void LoadImage()
        {
            const string url = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/Official_portrait_of_President_Obama_and_Vice_President_Biden_2012.jpg";
            const string file = "419px-Official_portrait_of_President_Obama_and_Vice_President_Biden_2012.jpg";

            var path = Path.Combine(ImageDirectory, file);
            if (!File.Exists(path))
            {
                var binary = new HttpClient().GetByteArrayAsync($"{url}/{file}").Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path, binary);
            }

            using (var array2D = DlibDotNet.Dlib.LoadImage<RgbPixel>(path))
            {
                var bytes = array2D.ToBytes();

                var image = FaceRecognition.LoadImage(bytes, array2D.Rows, array2D.Columns, 3);
                Assert.IsTrue(image.Width == 419, $"Width of {path} is wrong");
                Assert.IsTrue(image.Height == 600, $"Height of {path} is wrong");

                image.Dispose();
                Assert.IsTrue(image.IsDisposed, $"{typeof(Image)} should be already disposed.");

                try
                {
                    image.Dispose();
                }
                catch
                {
                    Assert.Fail($"{typeof(Image)} must not throw exception even though {nameof(Image.Dispose)} method is called again.");
                }

                try
                {
                    var _ = image.Width;
                    Assert.Fail($"{nameof(Image.Width)} must throw {typeof(ObjectDisposedException)} after object is disposed.");
                }
                catch
                {
                }

                try
                {
                    var _ = image.Height;
                    Assert.Fail($"{nameof(Image.Height)} must throw {typeof(ObjectDisposedException)} after object is disposed.");
                }
                catch
                {
                }
            }
        }

        [TestMethod]
        public void LoadImageGrayscale()
        {
            const string url = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/Official_portrait_of_President_Obama_and_Vice_President_Biden_2012.jpg";
            const string file = "419px-Official_portrait_of_President_Obama_and_Vice_President_Biden_2012.jpg";

            var path = Path.Combine(ImageDirectory, file);
            if (!File.Exists(path))
            {
                var binary = new HttpClient().GetByteArrayAsync($"{url}/{file}").Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path, binary);
            }

            using (var array2D = DlibDotNet.Dlib.LoadImage<RgbPixel>(path))
            using (var array2DGray = new Array2D<byte>(array2D.Rows, array2D.Columns))
            {
                DlibDotNet.Dlib.AssignImage(array2DGray, array2D);
                var bytes = array2DGray.ToBytes();

                using (var image = FaceRecognition.LoadImage(bytes, array2DGray.Rows, array2DGray.Columns, 1))
                {
                    Assert.IsTrue(image.Width == 419, $"Width of {path} is wrong");
                    Assert.IsTrue(image.Height == 600, $"Height of {path} is wrong");

                    image.Dispose();
                    Assert.IsTrue(image.IsDisposed, $"{typeof(Image)} should be already disposed.");

                    try
                    {
                        image.Dispose();
                    }
                    catch
                    {
                        Assert.Fail($"{typeof(Image)} must not throw exception even though {nameof(Image.Dispose)} method is called again.");
                    }

                    try
                    {
                        var _ = image.Width;
                        Assert.Fail($"{nameof(Image.Width)} must throw {typeof(ObjectDisposedException)} after object is disposed.");
                    }
                    catch
                    {
                    }

                    try
                    {
                        var _ = image.Height;
                        Assert.Fail($"{nameof(Image.Height)} must throw {typeof(ObjectDisposedException)} after object is disposed.");
                    }
                    catch
                    {
                    }
                }
            }
        }

        [TestMethod]
        public void LoadImageException()
        {
            try
            {
                var _ = FaceRecognition.LoadImage(null, 100, 100, 3);
                Assert.Fail($"{nameof(FaceRecognition.LoadImage)} must throw {typeof(ArgumentNullException)}.");
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                var _ = FaceRecognition.LoadImage(new byte[100 * 100 * 2], 100, 100, 2);
                Assert.Fail($"{nameof(FaceRecognition.LoadImage)} must throw {typeof(ArgumentOutOfRangeException)}.");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        [TestMethod]
        public void LoadImageCheckIdentity()
        {
            const string url = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/Official_portrait_of_President_Obama_and_Vice_President_Biden_2012.jpg";
            const string file = "419px-Official_portrait_of_President_Obama_and_Vice_President_Biden_2012.jpg";

            var path = Path.Combine(ImageDirectory, file);
            if (!File.Exists(path))
            {
                var binary = new HttpClient().GetByteArrayAsync($"{url}/{file}").Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path, binary);
            }

            var image1 = FaceRecognition.LoadImageFile(path);

            var array2D = DlibDotNet.Dlib.LoadImage<RgbPixel>(path);
            var bytes = array2D.ToBytes();
            var image2 = FaceRecognition.LoadImage(bytes, array2D.Rows, array2D.Columns, 3);

            var location1 = this._FaceRecognition.FaceLocations(image1).ToArray();
            var location2 = this._FaceRecognition.FaceLocations(image2).ToArray();

            Assert.IsTrue(location1.Length == location2.Length, $"FaceRecognition.FaceLocations returns different results for {nameof(location1)} and {nameof(location2)}.");

            for (var index = 0; index < location1.Length; index++)
            {
                Assert.IsTrue(location1[index] == location2[index],
                    $"{nameof(location1)}[{nameof(index)}] does not equal to {nameof(location2)}[{nameof(index)}].");
            }

            image2.Dispose();
            image1.Dispose();
            Assert.IsTrue(image2.IsDisposed, $"{nameof(image2)} should be already disposed.");
            Assert.IsTrue(image1.IsDisposed, $"{nameof(image1)} should be already disposed.");
        }

        [TestMethod]
        public void LoadImageFile()
        {
            const string url = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/Official_portrait_of_President_Obama_and_Vice_President_Biden_2012.jpg";
            const string file = "419px-Official_portrait_of_President_Obama_and_Vice_President_Biden_2012.jpg";

            var path = Path.Combine(ImageDirectory, file);
            if (!File.Exists(path))
            {
                var binary = new HttpClient().GetByteArrayAsync($"{url}/{file}").Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path, binary);
            }

            var image = FaceRecognition.LoadImageFile(path);
            Assert.IsTrue(image.Width == 419, $"Width of {path} is wrong");
            Assert.IsTrue(image.Height == 600, $"Height of {path} is wrong");

            image.Dispose();
            Assert.IsTrue(image.IsDisposed, $"{typeof(Image)} should be already disposed.");
        }

        [TestMethod]
        public void LoadImageFail()
        {
            try
            {
                FaceRecognition.LoadImageFile("test.bmp");
                Assert.Fail("test.bmp directory is missing and LoadImageFile method should throw exception.");
            }
            catch (FileNotFoundException)
            {
            }
        }

        [TestMethod]
        public void SerializeDeserializeBinaryFormatter()
        {
            const string testName = nameof(this.SerializeDeserializeBinaryFormatter);
            var directory = Path.Combine(ResultDirectory, testName);
            Directory.CreateDirectory(directory);

            var path = Path.Combine(ImageDirectory, TwoPersonFile);
            if (!File.Exists(path))
            {
                var binary = new HttpClient().GetByteArrayAsync($"{TwoPersonUrl}/{TwoPersonFile}").Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path, binary);
            }

            using (var image = FaceRecognition.LoadImageFile(path))
            {
                var encodings = this._FaceRecognition.FaceEncodings(image).ToArray();
                Assert.IsTrue(encodings.Length > 1, "");

                var dest = $"{path}.dat";
                if (File.Exists(dest))
                    File.Delete(dest);

                var bf = new BinaryFormatter();
                using (var fs = new FileStream(dest, FileMode.OpenOrCreate))
                    bf.Serialize(fs, encodings.First());

                using (var fs = new FileStream(dest, FileMode.OpenOrCreate))
                {
                    var encoding = (FaceEncoding)bf.Deserialize(fs);
                    var distance = FaceRecognition.FaceDistance(encodings.First(), encoding);
                    Assert.IsTrue(Math.Abs(distance) < double.Epsilon);
                }

                foreach (var encoding in encodings)
                    encoding.Dispose();

                foreach (var encoding in encodings)
                    Assert.IsTrue(encoding.IsDisposed, $"{typeof(FaceEncoding)} should be already disposed.");
            }
        }

        [TestMethod]
        public void PredictAge()
        {
            if (this._HasAgeDataset)
            {
                // 0: (0, 2)
                // 1: (4, 6)
                // 2: (8, 13)
                // 3: (15, 20)
                // 4: (25, 32)
                // 5: (38, 43)
                // 6: (48, 53)
                // 7: (60, 100)
                var groundTruth = new[]
                {
                    new { Path = @"TestImages\Age\NelsonMandela_2008_90.jpg",        Age = new uint[]{ 7 } },
                    new { Path = @"TestImages\Age\MacaulayCulkin_1991_11.jpg",       Age = new uint[]{ 2, 3 } },
                    new { Path = @"TestImages\Age\DianaPrincessOfWales_1997_36.jpg", Age = new uint[]{ 4, 5 } },
                    new { Path = @"TestImages\Age\MaoAsada_2014_24.jpg",             Age = new uint[]{ 3, 4 } }
                };

                foreach (var gt in groundTruth)
                    using (var image = FaceRecognition.LoadImageFile(gt.Path))
                    {
                        var location = this._FaceRecognition.FaceLocations(image).ToArray()[0];
                        var age = this._FaceRecognition.PredictAge(image, location);
                        Assert.IsTrue(gt.Age.Contains(age), $"Failed to classify '{gt.Path}'");
                    }
            }
        }

        [TestMethod]
        public void PredictGender()
        {
            if (this._HasGenderDataset)
            {
                var groundTruth = new[]
                {
                    new { Path = @"TestImages\Gender\BarackObama_male.jpg",            Gender = Gender.Male },
                    new { Path = @"TestImages\Gender\DianaPrincessOfWales_female.jpg", Gender = Gender.Female },
                    new { Path = @"TestImages\Gender\MaoAsada_female.jpg",             Gender = Gender.Female },
                    new { Path = @"TestImages\Gender\ShinzoAbe_male.jpg",              Gender = Gender.Male },
                    new { Path = @"TestImages\Gender\WhitneyHouston_female.jpg",       Gender = Gender.Female },
                };

                foreach (var gt in groundTruth)
                    using (var image = FaceRecognition.LoadImageFile(gt.Path))
                    {
                        var location = this._FaceRecognition.FaceLocations(image).ToArray()[0];
                        var gender = this._FaceRecognition.PredictGender(image, location);
                        Assert.IsTrue(gt.Gender == gender, $"Failed to classify '{gt.Path}'");
                    }
            }
        }

        [TestMethod]
        public void PredictProbabilityAge()
        {
            if (this._HasAgeDataset)
            {
                // 0: (0, 2)
                // 1: (4, 6)
                // 2: (8, 13)
                // 3: (15, 20)
                // 4: (25, 32)
                // 5: (38, 43)
                // 6: (48, 53)
                // 7: (60, 100)
                var groundTruth = new[]
                {
                    new { Path = @"TestImages\Age\NelsonMandela_2008_90.jpg",        Age = new uint[]{ 7 } },
                    new { Path = @"TestImages\Age\MacaulayCulkin_1991_11.jpg",       Age = new uint[]{ 2, 3 } },
                    new { Path = @"TestImages\Age\DianaPrincessOfWales_1997_36.jpg", Age = new uint[]{ 4, 5 } },
                    new { Path = @"TestImages\Age\MaoAsada_2014_24.jpg",             Age = new uint[]{ 3, 4 } }
                };

                foreach (var gt in groundTruth)
                    using (var image = FaceRecognition.LoadImageFile(gt.Path))
                    {
                        var location = this._FaceRecognition.FaceLocations(image).ToArray()[0];
                        var probability = this._FaceRecognition.PredictProbabilityAge(image, location);

                        // Take top 2
                        var order = probability.OrderByDescending(x => x.Value).Take(2).ToArray();
                        var any = order.Select(pair => pair.Key).Any(u => gt.Age.Contains(u));
                        Assert.IsTrue(any, $"Failed to classify '{gt.Path}'. Probability: 1 ({order[0].Key}-{order[0].Value}), 2 ({order[1].Key}-{order[1].Value})");
                    }
            }
        }

        [TestMethod]
        public void PredictProbabilityGender()
        {
            if (this._HasGenderDataset)
            {
                var groundTruth = new[]
                {
                    new { Path = @"TestImages\Gender\BarackObama_male.jpg",            Gender = Gender.Male },
                    new { Path = @"TestImages\Gender\DianaPrincessOfWales_female.jpg", Gender = Gender.Female },
                    new { Path = @"TestImages\Gender\MaoAsada_female.jpg",             Gender = Gender.Female },
                    new { Path = @"TestImages\Gender\ShinzoAbe_male.jpg",              Gender = Gender.Male },
                    new { Path = @"TestImages\Gender\WhitneyHouston_female.jpg",       Gender = Gender.Female },
                };

                foreach (var gt in groundTruth)
                    using (var image = FaceRecognition.LoadImageFile(gt.Path))
                    {
                        var location = this._FaceRecognition.FaceLocations(image).ToArray()[0];
                        var probability = this._FaceRecognition.PredictProbabilityGender(image, location);

                        var pos = gt.Gender;
                        var neg = pos == Gender.Male ? Gender.Female : Gender.Male;
                        Assert.IsTrue(probability[pos] > probability[neg], $"Failed to classify '{gt.Path}'. Probability: {probability[pos]}");
                    }
            }
        }

        [TestMethod]
        public void TestBatchedFaceLocations()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                var images = new[] { img, img, img };
                var batchedDetectedFaces = this._FaceRecognition.BatchFaceLocations(images, 0).ToArray();
                Assert.AreEqual(3, batchedDetectedFaces.Length);

                foreach (var detectedFaces in batchedDetectedFaces)
                {
                    var tmp = detectedFaces.ToArray();
                    Assert.IsTrue(tmp.Length == 1);
                    Assert.IsTrue(tmp[0] == new Location(375, 154, 611, 390));
                }
            }
        }

        [TestMethod]
        public void TestBatchedFaceLocationsException()
        {
            using (var _ = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                var images = new Image[] { };

                try
                {
                    var __ = this._FaceRecognition.BatchFaceLocations(images, 0).ToArray();
                }
                catch (Exception)
                {
                    Assert.Fail($"{nameof(FaceRecognition.BatchFaceLocations)} must not throw exception even though {nameof(images)} is empty elements.");
                }

                try
                {
                    var __ = this._FaceRecognition.BatchFaceLocations(null, 0).ToArray();
                    Assert.Fail($"{nameof(FaceRecognition.BatchFaceLocations)} must throw {typeof(ArgumentNullException)}.");
                }
                catch (ArgumentNullException)
                {
                }
            }
        }

        [TestMethod]
        public void TestCnnFaceLocations()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                var detectedFaces = this._FaceRecognition.FaceLocations(img, 1, Model.Cnn).ToArray();
                Assert.IsTrue(detectedFaces.Length == 1);
                Assert.IsTrue(AssertAlmostEqual(detectedFaces[0].Top, 144, 25));
                Assert.IsTrue(AssertAlmostEqual(detectedFaces[0].Right, 608, 25));
                Assert.IsTrue(AssertAlmostEqual(detectedFaces[0].Bottom, 389, 25));
                Assert.IsTrue(AssertAlmostEqual(detectedFaces[0].Left, 363, 25));
            }
        }

        [TestMethod]
        public void TestCnnRawFaceLocations()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                var detectedFaces = this.RawFaceLocations(img, 1, Model.Cnn).ToArray();
                Assert.IsTrue(detectedFaces.Length == 1);
                Assert.IsTrue(AssertAlmostEqual(detectedFaces[0].Rect.Top, 144, 25));
                Assert.IsTrue(AssertAlmostEqual(detectedFaces[0].Rect.Bottom, 389, 25));
            }
        }

        [TestMethod]
        public void TestCnnRawFaceLocations32BitImage()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "32bit.png")))
            {
                var detectedFaces = this.RawFaceLocations(img, 1, Model.Cnn).ToArray();
                Assert.IsTrue(detectedFaces.Length == 1);
                Assert.IsTrue(AssertAlmostEqual(detectedFaces[0].Rect.Top, 259, 25));
                Assert.IsTrue(AssertAlmostEqual(detectedFaces[0].Rect.Bottom, 552, 25));
            }
        }

        [TestMethod]
        public void TestCompareFaces()
        {
            using (var imgA1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            using (var imgA2 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama2.jpg")))
            using (var imgA3 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama3.jpg")))
            using (var imgB1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "biden.jpg")))
            {
                using (var faceEncodingA1 = this._FaceRecognition.FaceEncodings(imgA1).ToArray()[0])
                using (var faceEncodingA2 = this._FaceRecognition.FaceEncodings(imgA2).ToArray()[0])
                using (var faceEncodingA3 = this._FaceRecognition.FaceEncodings(imgA3).ToArray()[0])
                using (var faceEncodingB1 = this._FaceRecognition.FaceEncodings(imgB1).ToArray()[0])
                {
                    var facesToCompare = new[]
                    {
                        faceEncodingA2,
                        faceEncodingA3,
                        faceEncodingB1
                    };

                    var matchResults = facesToCompare.Select(faceToCompare => FaceRecognition.CompareFace(faceToCompare, faceEncodingA1)).ToList();

                    Assert.IsTrue(matchResults[0]);
                    Assert.IsTrue(matchResults[1]);
                    Assert.IsFalse(matchResults[2]);
                }
            }
        }

        [TestMethod]
        public void TestCompareFaceException()
        {
            using (var imgA1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            using (var imgA2 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama2.jpg")))
            {
                using (var faceEncodingA1 = this._FaceRecognition.FaceEncodings(imgA1).ToArray()[0])
                using (var faceEncodingA2 = this._FaceRecognition.FaceEncodings(imgA2).ToArray()[0])
                {
                    try
                    {
                        FaceRecognition.CompareFace(faceEncodingA1, null);
                        Assert.Fail($"{nameof(FaceRecognition.CompareFace)} must throw {typeof(ArgumentNullException)}.");
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        FaceRecognition.CompareFace(null, faceEncodingA2);
                        Assert.Fail($"{nameof(FaceRecognition.CompareFace)} must throw {typeof(ArgumentNullException)}.");
                    }
                    catch (ArgumentNullException)
                    {
                    }
                }
            }
        }

        [TestMethod]
        public void TestCompareFacesException()
        {
            using (var imgA1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            using (var imgA2 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama2.jpg")))
            using (var imgA3 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama3.jpg")))
            using (var imgB1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "biden.jpg")))
            {
                using (var faceEncodingA1 = this._FaceRecognition.FaceEncodings(imgA1).ToArray()[0])
                using (var faceEncodingA2 = this._FaceRecognition.FaceEncodings(imgA2).ToArray()[0])
                using (var faceEncodingA3 = this._FaceRecognition.FaceEncodings(imgA3).ToArray()[0])
                using (var faceEncodingB1 = this._FaceRecognition.FaceEncodings(imgB1).ToArray()[0])
                {
                    var facesToCompare = new[]
                    {
                        faceEncodingA2,
                        faceEncodingA3,
                        faceEncodingB1
                    };

                    try
                    {
                        var _ = FaceRecognition.CompareFaces(facesToCompare, null).ToArray();
                        Assert.Fail($"{nameof(FaceRecognition.CompareFaces)} must throw {typeof(ArgumentNullException)}.");
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        var _ = FaceRecognition.CompareFaces(null, faceEncodingA1).ToArray();
                        Assert.Fail($"{nameof(FaceRecognition.CompareFaces)} must throw {typeof(ArgumentNullException)}.");
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        foreach (var encoding in facesToCompare)
                            encoding.Dispose();
                        var _ = FaceRecognition.CompareFaces(facesToCompare, faceEncodingA1).ToArray();
                        Assert.Fail($"{nameof(FaceRecognition.CompareFaces)} must throw {typeof(ObjectDisposedException)} if knownFaceEncodings contains disposed object.");
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }

        [TestMethod]
        public void TestCompareFacesEmptyLists()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "biden.jpg")))
            {
                var encoding = this._FaceRecognition.FaceEncodings(img).ToArray()[0];

                // empty list 
                var facesToCompare = new FaceEncoding[0];

                var matchResult = FaceRecognition.CompareFaces(facesToCompare, encoding).ToArray();
                Assert.IsTrue(matchResult.Length == 0);
            }
        }

        [TestMethod]
        public void TestFaceDistance()
        {
            using (var imgA1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            using (var imgA2 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama2.jpg")))
            using (var imgA3 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama3.jpg")))
            using (var imgB1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "biden.jpg")))
            {
                using (var faceEncodingA1 = this._FaceRecognition.FaceEncodings(imgA1).ToArray()[0])
                using (var faceEncodingA2 = this._FaceRecognition.FaceEncodings(imgA2).ToArray()[0])
                using (var faceEncodingA3 = this._FaceRecognition.FaceEncodings(imgA3).ToArray()[0])
                using (var faceEncodingB1 = this._FaceRecognition.FaceEncodings(imgB1).ToArray()[0])
                {
                    var facesToCompare = new[]
                    {
                        faceEncodingA2,
                        faceEncodingA3,
                        faceEncodingB1
                    };

                    var distanceResults = facesToCompare.Select(faceToCompare => FaceRecognition.FaceDistance(faceToCompare, faceEncodingA1)).ToList();

                    Assert.IsTrue(distanceResults[0] <= 0.6);
                    Assert.IsTrue(distanceResults[1] <= 0.6);
                    Assert.IsTrue(distanceResults[2] > 0.6);
                }
            }
        }

        [TestMethod]
        public void TestFaceDistances()
        {
            using (var imgA1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            using (var imgA2 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama2.jpg")))
            using (var imgA3 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama3.jpg")))
            using (var imgB1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "biden.jpg")))
            {
                using (var faceEncodingA1 = this._FaceRecognition.FaceEncodings(imgA1).ToArray()[0])
                using (var faceEncodingA2 = this._FaceRecognition.FaceEncodings(imgA2).ToArray()[0])
                using (var faceEncodingA3 = this._FaceRecognition.FaceEncodings(imgA3).ToArray()[0])
                using (var faceEncodingB1 = this._FaceRecognition.FaceEncodings(imgB1).ToArray()[0])
                {
                    var facesToCompare = new[]
                    {
                        faceEncodingA2,
                        faceEncodingA3,
                        faceEncodingA1
                    };

                    var results = FaceRecognition.FaceDistances(facesToCompare, faceEncodingB1).ToArray();
                    Assert.IsTrue(!results.Any(d => d < 0.8));

                    facesToCompare = new[]
                    {
                        faceEncodingA2,
                        faceEncodingA3
                    };

                    var results2 = FaceRecognition.FaceDistances(facesToCompare, faceEncodingA1).ToArray();
                    Assert.IsTrue(!results2.Any(d => d > 0.4));
                }
            }
        }

        [TestMethod]
        public void FaceDistanceException()
        {
            using (var imgA1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            using (var imgA2 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama2.jpg")))
            {
                using (var faceEncodingA1 = this._FaceRecognition.FaceEncodings(imgA1).ToArray()[0])
                using (var faceEncodingA2 = this._FaceRecognition.FaceEncodings(imgA2).ToArray()[0])
                {
                    try
                    {
                        var _ = FaceRecognition.FaceDistance(faceEncodingA1, null);
                        Assert.Fail($"{nameof(FaceRecognition.FaceDistance)} must throw {typeof(ArgumentNullException)}.");
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        var _ = FaceRecognition.FaceDistance(null, faceEncodingA2);
                        Assert.Fail($"{nameof(FaceRecognition.FaceDistance)} must throw {typeof(ArgumentNullException)}.");
                    }
                    catch (ArgumentNullException)
                    {
                    }
                }
            }
        }

        [TestMethod]
        public void FaceDistancesException()
        {
            using (var imgA1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            using (var imgA2 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama2.jpg")))
            using (var imgA3 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama3.jpg")))
            using (var imgB1 = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "biden.jpg")))
            {
                using (var faceEncodingA1 = this._FaceRecognition.FaceEncodings(imgA1).ToArray()[0])
                using (var faceEncodingA2 = this._FaceRecognition.FaceEncodings(imgA2).ToArray()[0])
                using (var faceEncodingA3 = this._FaceRecognition.FaceEncodings(imgA3).ToArray()[0])
                using (var faceEncodingB1 = this._FaceRecognition.FaceEncodings(imgB1).ToArray()[0])
                {
                    var facesToCompare = new[]
                    {
                        faceEncodingA2,
                        faceEncodingA3,
                        faceEncodingB1
                    };

                    try
                    {
                        var _ = FaceRecognition.FaceDistances(facesToCompare, null).ToArray();
                        Assert.Fail($"{nameof(FaceRecognition.FaceDistances)} must throw {typeof(ArgumentNullException)}.");
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        var _ = FaceRecognition.FaceDistances(null, faceEncodingA1).ToArray();
                        Assert.Fail($"{nameof(FaceRecognition.FaceDistances)} must throw {typeof(ArgumentNullException)}.");
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        foreach (var encoding in facesToCompare)
                            encoding.Dispose();
                        var _ = FaceRecognition.FaceDistances(facesToCompare, faceEncodingA1).ToArray();
                        Assert.Fail($"{nameof(FaceRecognition.FaceDistances)} must throw {typeof(ObjectDisposedException)} if faceEncodings contains disposed object.");
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }

        [TestMethod]
        public void TestFaceDistanceEmptyLists()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "biden.jpg")))
            {
                var encoding = this._FaceRecognition.FaceEncodings(img).ToArray()[0];

                // empty list 
                var facesToCompare = new FaceEncoding[0];

                var distanceResult = FaceRecognition.FaceDistances(facesToCompare, encoding).ToArray();
                Assert.IsTrue(distanceResult.Length == 0);
            }
        }

        [TestMethod]
        public void TestFaceEncodings()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                var encodings = this._FaceRecognition.FaceEncodings(img).ToArray();
                Assert.IsTrue(encodings.Length == 1);
                Assert.IsTrue(encodings[0].Size == 128);
            }
        }

        [TestMethod]
        public void TestFaceLandmarks()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                var faceLandmarks = this._FaceRecognition.FaceLandmark(img).ToArray();

                var parts = new[]
                {
                    FacePart.Chin,
                    FacePart.LeftEyebrow,
                    FacePart.RightEyebrow,
                    FacePart.NoseBridge,
                    FacePart.NoseTip,
                    FacePart.LeftEye,
                    FacePart.RightEye,
                    FacePart.TopLip,
                    FacePart.BottomLip
                };

                foreach (var facePart in faceLandmarks[0].Keys)
                    if (!parts.Contains(facePart))
                        Assert.Fail();

                var points = new[]
                {
                    new Point(369, 220),
                    new Point(372, 254),
                    new Point(378, 289),
                    new Point(384, 322),
                    new Point(395, 353),
                    new Point(414, 382),
                    new Point(437, 407),
                    new Point(464, 424),
                    new Point(495, 428),
                    new Point(527, 420),
                    new Point(552, 399),
                    new Point(576, 372),
                    new Point(594, 344),
                    new Point(604, 314),
                    new Point(610, 282),
                    new Point(613, 250),
                    new Point(615, 219)
                };

                var facePartPoints = faceLandmarks[0][FacePart.Chin].ToArray();
                for (var index = 0; index < facePartPoints.Length; index++)
                    Assert.IsTrue(facePartPoints[index] == points[index]);
            }
        }

        [TestMethod]
        public void TestFaceLandmarksSmallModel()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                var faceLandmarks = this._FaceRecognition.FaceLandmark(img, null, PredictorModel.Small).ToArray();

                var parts = new[]
                {
                    FacePart.NoseTip,
                    FacePart.LeftEye,
                    FacePart.RightEye
                };

                foreach (var facePart in faceLandmarks[0].Keys)
                    if (!parts.Contains(facePart))
                        Assert.Fail();

                var points = new[]
                {
                    new Point(496, 295)
                };

                var facePartPoints = faceLandmarks[0][FacePart.NoseTip].ToArray();
                for (var index = 0; index < facePartPoints.Length; index++)
                    Assert.IsTrue(facePartPoints[index] == points[index]);
            }
        }

        [TestMethod]
        public void TestFaceLocations()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                var detectedFaces = this._FaceRecognition.FaceLocations(img).ToArray();
                Assert.IsTrue(detectedFaces.Length == 1);
                Assert.IsTrue(detectedFaces[0].Top == 142);
                Assert.IsTrue(detectedFaces[0].Right == 617);
                Assert.IsTrue(detectedFaces[0].Bottom == 409);
                Assert.IsTrue(detectedFaces[0].Left == 349);
            }
        }

        [TestMethod]
        public void TestLoadImageFile()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                Assert.IsTrue(img.Height == 1137);
                Assert.IsTrue(img.Width == 910);
            }
        }

        [TestMethod]
        public void TestLoadImageFile32Bit()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "32bit.png")))
            {
                Assert.IsTrue(img.Height == 1200);
                Assert.IsTrue(img.Width == 626);
            }
        }

        [TestMethod]
        public void TestPartialFaceLocations()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama_partial_face.jpg")))
            {
                var detectedFaces = this._FaceRecognition.FaceLocations(img).ToArray();
                Assert.IsTrue(detectedFaces.Length == 1);
                Assert.IsTrue(detectedFaces[0].Top == 142);
                Assert.IsTrue(detectedFaces[0].Right == 191);
                Assert.IsTrue(detectedFaces[0].Bottom == 365);
                Assert.IsTrue(detectedFaces[0].Left == 0);
            }

            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama_partial_face2.jpg")))
            {
                var detectedFaces = this._FaceRecognition.FaceLocations(img).ToArray();
                Assert.IsTrue(detectedFaces.Length == 1);
                Assert.IsTrue(detectedFaces[0].Top == 142);
                Assert.IsTrue(detectedFaces[0].Right == 551);
                Assert.IsTrue(detectedFaces[0].Bottom == 409);
                Assert.IsTrue(detectedFaces[0].Left == 349);
            }
        }

        [TestMethod]
        public void TestRawFaceLandmarks()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                var faceLandmarks = this.RawFaceLandmarks(img).ToArray();
                var exampleLandmark = faceLandmarks[0].GetPart(10);

                Assert.IsTrue(faceLandmarks.Length == 1);
                Assert.IsTrue(faceLandmarks[0].Parts == 68);
                Assert.IsTrue(exampleLandmark.X == 552);
                Assert.IsTrue(exampleLandmark.Y == 399);
            }
        }

        [TestMethod]
        public void TestRawFaceLocations()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                var detectedFaces = this.RawFaceLocations(img).ToArray();
                Assert.IsTrue(detectedFaces.Length == 1);
                Assert.IsTrue(detectedFaces[0].Rect.Top == 142);
                Assert.IsTrue(detectedFaces[0].Rect.Bottom == 409);
            }
        }

        [TestMethod]
        public void TestRawFaceLocations32BitImage()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "32bit.png")))
            {
                var detectedFaces = this.RawFaceLocations(img).ToArray();
                Assert.IsTrue(detectedFaces.Length == 1);
                Assert.IsTrue(detectedFaces[0].Rect.Top == 290);
                Assert.IsTrue(detectedFaces[0].Rect.Bottom == 558);
            }
        }

        [TestMethod]
        public void TestRawFaceLocationsBatched()
        {
            using (var img = FaceRecognition.LoadImageFile(Path.Combine("TestImages", "obama.jpg")))
            {
                var images = new[] { img, img, img };
                var batchedDetectedFaces = this.RawFaceLocationsBatched(images, 0).ToArray();

                foreach (var detectedFaces in batchedDetectedFaces)
                {
                    var tmp = detectedFaces.ToArray();
                    Assert.IsTrue(tmp.Length == 1);
                    Assert.IsTrue(tmp[0].Rect.Top == 154);
                    Assert.IsTrue(tmp[0].Rect.Bottom == 390);
                }
            }
        }

        #region Helpers 

        private static bool AssertAlmostEqual(int actual, int expected, int delta)
        {
            return expected - delta <= actual && actual <= expected + delta;
        }

        private void FaceLandmark(string testName, PredictorModel model, bool useKnownLocation)
        {
            const int pointSize = 2;

            var path = Path.Combine(ImageDirectory, TwoPersonFile);
            if (!File.Exists(path))
            {
                var binary = new HttpClient().GetByteArrayAsync($"{TwoPersonUrl}/{TwoPersonFile}").Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path, binary);
            }

            foreach (var mode in new[] { Mode.Rgb, Mode.Greyscale })
            {
                using (var image = FaceRecognition.LoadImageFile(path, mode))
                {
                    IEnumerable<Location> knownLocations = null;
                    if (useKnownLocation)
                        knownLocations = this._FaceRecognition.FaceLocations(image).ToArray();

                    var landmarks = this._FaceRecognition.FaceLandmark(image, knownLocations, model).ToArray();
                    Assert.IsTrue(landmarks.Length > 1, $"{mode}");

                    foreach (var facePart in Enum.GetValues(typeof(FacePart)).Cast<FacePart>())
                        using (var bitmap = System.Drawing.Image.FromFile(path))
                        {
                            var draw = false;
                            using (var g = Graphics.FromImage(bitmap))
                                foreach (var landmark in landmarks)
                                    if (landmark.ContainsKey(facePart))
                                    {
                                        draw = true;
                                        foreach (var p in landmark[facePart].ToArray())
                                            g.DrawEllipse(Pens.GreenYellow, p.X - pointSize, p.Y - pointSize, pointSize * 2, pointSize * 2);
                                    }

                            if (draw)
                            {
                                var directory = Path.Combine(ResultDirectory, testName);
                                Directory.CreateDirectory(directory);

                                var dst = Path.Combine(directory, $"{facePart}-{mode}-known_{useKnownLocation}.bmp");
                                bitmap.Save(dst, ImageFormat.Bmp);
                            }
                        }

                    using (var bitmap = System.Drawing.Image.FromFile(path))
                    {
                        using (var g = Graphics.FromImage(bitmap))
                            foreach (var landmark in landmarks)
                                foreach (var points in landmark.Values)
                                    foreach (var p in points)
                                    {
                                        g.DrawEllipse(Pens.GreenYellow, p.X - pointSize, p.Y - pointSize, pointSize * 2, pointSize * 2);
                                    }

                        var directory = Path.Combine(ResultDirectory, testName);
                        Directory.CreateDirectory(directory);

                        var dst = Path.Combine(directory, $"All-{mode}-known_{useKnownLocation}.bmp");
                        bitmap.Save(dst, ImageFormat.Bmp);
                    }
                }
            }
        }

        private void FaceLocation(string testName, int numberOfTimesToUpsample, Model model)
        {
            var path = Path.Combine(ImageDirectory, TwoPersonFile);
            if (!File.Exists(path))
            {
                var binary = new HttpClient().GetByteArrayAsync($"{TwoPersonUrl}/{TwoPersonFile}").Result;

                Directory.CreateDirectory(ImageDirectory);
                File.WriteAllBytes(path, binary);
            }

            foreach (var mode in new[] { Mode.Rgb, Mode.Greyscale })
            {
                using (var image = FaceRecognition.LoadImageFile(path, mode))
                {
                    var locations = this._FaceRecognition.FaceLocations(image, numberOfTimesToUpsample, model).ToArray();
                    Assert.IsTrue(locations.Length > 1, $"{mode}");

                    using (var bitmap = System.Drawing.Image.FromFile(path))
                    {
                        using (var g = Graphics.FromImage(bitmap))
                            foreach (var l in locations)
                                g.DrawRectangle(Pens.GreenYellow, l.Left, l.Top, l.Right - l.Left, l.Bottom - l.Top);

                        var directory = Path.Combine(ResultDirectory, testName);
                        Directory.CreateDirectory(directory);

                        var dst = Path.Combine(directory, $"All-{mode}-{numberOfTimesToUpsample}.bmp");
                        bitmap.Save(dst, ImageFormat.Bmp);
                    }
                }
            }
        }

        private IEnumerable<FullObjectDetection> RawFaceLandmarks(Image img, IEnumerable<Location> faceLocations = null, PredictorModel model = PredictorModel.Large)
        {
            var method = this._FaceRecognition.GetType().GetMethod("RawFaceLandmarks", BindingFlags.Instance | BindingFlags.NonPublic);
            return method.Invoke(this._FaceRecognition, new object[] { img, faceLocations, model }) as IEnumerable<FullObjectDetection>;
        }

        private IEnumerable<MModRect> RawFaceLocations(Image img, int numberOfTimesToUpsample = 1, Model model = Model.Hog)
        {
            var method = this._FaceRecognition.GetType().GetMethod("RawFaceLocations", BindingFlags.Instance | BindingFlags.NonPublic);
            return method.Invoke(this._FaceRecognition, new object[] { img, numberOfTimesToUpsample, model }) as IEnumerable<MModRect>;
        }

        private IEnumerable<IEnumerable<MModRect>> RawFaceLocationsBatched(IEnumerable<Image> faceImages, int numberOfTimesToUpsample = 1, int batchSize = 128)
        {
            var method = this._FaceRecognition.GetType().GetMethod("RawFaceLocationsBatched", BindingFlags.Instance | BindingFlags.NonPublic);
            return method.Invoke(this._FaceRecognition, new object[] { faceImages, numberOfTimesToUpsample, batchSize }) as IEnumerable<IEnumerable<MModRect>>;
        }

        #endregion

        #endregion

    }

}