using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PuppeteerSharp;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Reflection.Metadata;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Engine3D
{
    public class AssetToDownload
    {
        public string webPath = "";
        public Asset asset;

        // Full
        public string fullWebPath = "";
        public string outputDirectory = "";
        public string fileName;
        public int fileSize;

        public AssetToDownload(Asset asset, string webPath)
        {
            this.asset = asset;
            this.webPath = webPath;
        }

        public AssetToDownload(string fullWebPath, string outputDirectory, string fileName, int fileSize)
        {
            this.fullWebPath = fullWebPath;
            this.outputDirectory = outputDirectory;
            this.fileName = fileName;
            this.fileSize = fileSize;
        }
    }

    public class AssetStoreManager
    {
        public string currentKeyword = "";
        public int currentPageNumber = 0;
        public List<Asset> assets;
        private AssetManager assetManager;

        private List<AssetToDownload> assetToDownloadsPreview;
        private List<AssetToDownload> assetToDownloadsFull;

        private List<AssetToDownload> assetZipToDownload;

        private readonly HttpClient _httpClient = new HttpClient();
        private volatile bool _isDownloadInProgress = false;
        public bool IsDownloadInProgress
        {
            get => _isDownloadInProgress;
            private set => _isDownloadInProgress = value;
        }

        public string ZipDownloadProgress
        {
            get
            {
                return "All downloads: " + assetZipToDownload.Count() + " Downloading: " + Math.Round(zipProgress * 100, 2) + "%%";
            }
        }

        private volatile float zipProgress = 0.0f;
        private volatile bool _isZipDownloadInProgress = false;
        public bool IsZipDownloadInProgress
        {
            get => _isZipDownloadInProgress;
            private set => _isZipDownloadInProgress = value;
        }

        public bool tryingToDownload = false;

        public bool IsThereInternetConnection = false;
        private Timer isThereInternetChecker;
        private const int checkingInterval = 10000;

        public AssetStoreManager(ref AssetManager assetManager)
        {
            assets = new List<Asset>();
            assetToDownloadsPreview = new List<AssetToDownload>();
            assetToDownloadsFull = new List<AssetToDownload>();
            assetZipToDownload = new List<AssetToDownload>(); 
            this.assetManager = assetManager;
            isThereInternetChecker = new Timer(InternetCheckerCallback, null, 0, checkingInterval);
        }

        public void Delete()
        {
            isThereInternetChecker.Dispose();
        }

        private void InternetCheckerCallback(object? state)
        {
            IsThereInternetConnection = IsInternetAvailable();
        }

        public async void GetOpenGameArtOrg()
        {
            if (currentKeyword == "")
            {
                assetManager.Remove(assets);
                assets.Clear();

                DeleteFolderContent("Temp");
                return;
            }

            IsThereInternetConnection = IsInternetAvailable();

            if (IsThereInternetConnection)
            {
                var url = "https://opengameart.org/art-search-advanced?keys=" + currentKeyword + "&title=&field_art_tags_tid_op=or&field_art_tags_tid=&name=&field_art_type_tid%5B0%5D=14&sort_by=count&sort_order=DESC&items_per_page=24&Collection=";
                if (currentPageNumber != 0)
                    url += "&page=" + currentPageNumber.ToString();

                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), url))
                    {
                        var response = await httpClient.SendAsync(request);
                        var content = await response.Content.ReadAsStringAsync();
                        ParseOpenGameArt(content);
                    }
                }
            }
        }

        private void ParseOpenGameArt(string content)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);

            var nodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='ds-1col node node-art view-mode-art_preview clearfix ']");
            if(nodes != null)
            {
                if(assets.Count > 0)
                {
                    assetManager.Remove(assets);
                    assets.Clear();

                    DeleteFolderContent("Temp");
                }
                else if(Directory.Exists(Environment.CurrentDirectory + "\\Assets\\Temp") && new DirectoryInfo(Environment.CurrentDirectory + "\\Assets\\Temp").GetFiles().Count() > 0)
                {
                    DeleteFolderContent("Temp");
                }

                foreach( var node in nodes )
                {
                    string name = WebUtility.HtmlDecode(node.InnerText).TrimStart('\n').TrimStart(' ').TrimStart('\n').TrimStart(' ');
                    HtmlNode imgNode = node.Descendants("img").First();
                    if (imgNode == null)
                        continue;

                    string imgSrc = imgNode.GetAttributeValue("src", string.Empty);

                    if (imgSrc == "")
                        continue;

                    string contentPath = "https://opengameart.org" + node.Descendants("a").First().GetAttributeValue("href", string.Empty);

                    if (contentPath == "")
                        continue;

                    Asset asset = new Asset(Asset.CurrentId + 1, name, "", AssetType.Texture, AssetTypeEditor.Store, FileType.Textures);
                    asset.WebPathFull = contentPath;
                    assetToDownloadsPreview.Add(new AssetToDownload(asset, imgSrc));
                }
                assetManager.Add(assets);
            }
        }

        public async void DownloadAssetFull(Asset asset)
        {
            IsThereInternetConnection = IsInternetAvailable();

            if (IsThereInternetConnection)
            {
                tryingToDownload = true;
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), asset.WebPathFull))
                    {
                        var response = await httpClient.SendAsync(request);
                        var content = await response.Content.ReadAsStringAsync();

                        var htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(content);

                        var node = htmlDoc.DocumentNode.SelectSingleNode("//span[@class='file']");
                        if (node == null)
                        {
                            // TODO: Console log error
                            return;
                        }

                        string fileWebPath = node.Descendants("a").First().GetAttributeValue("href", string.Empty);

                        var regex = new Regex(@"\s*(?<filename>.+?)\s+(?<size>\d+(\.\d+)?\s+[KMG]?b)\s+\[(?<downloads>\d+)\s+download\(s\)\]", RegexOptions.IgnoreCase);
                        var match = regex.Match(node.InnerText);

                        string filename = "";
                        int size = 0;
                        string downloads = "";
                        if (match.Success)
                        {
                            filename = match.Groups["filename"].Value;
                            size = (int)(float.Parse(match.Groups["size"].Value.Split(' ')[0]) * 1000);
                            downloads = match.Groups["downloads"].Value;
                        }

                        if (!File.Exists(Environment.CurrentDirectory + "\\Assets\\Temp\\" + filename))
                        {
                            AssetToDownload assetToDownload = new AssetToDownload(fileWebPath, Environment.CurrentDirectory + "\\Assets\\Temp", filename, size);
                            assetZipToDownload.Add(assetToDownload);
                        }
                    }
                }
            }
        }

        private async Task DownloadFileAsync(string url, string outputPath, Action<float> reportProgress)
        {
            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var totalRead = 0L;
                var buffer = new byte[8192];
                var isMoreToRead = true;

                using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                {
                    do
                    {
                        var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            isMoreToRead = false;
                        }
                        else
                        {
                            await fileStream.WriteAsync(buffer, 0, read);

                            totalRead += read;
                            if (totalBytes != -1L)
                            {
                                zipProgress = (float)totalRead / totalBytes;
                                reportProgress(zipProgress);
                            }
                        }
                    }
                    while (isMoreToRead);
                }
            }
        }

        public async void DownloadIfNeeded()
        {
            if (IsThereInternetConnection)
            {
                if (assetToDownloadsPreview.Count > 0)
                {
                    if (!Directory.Exists(Environment.CurrentDirectory + "\\Assets\\Temp"))
                        Directory.CreateDirectory(Environment.CurrentDirectory + "\\Assets\\Temp");

                    try
                    {
                        AssetToDownload assetToDownload = assetToDownloadsPreview[0];

                        if (!IsDownloadInProgress)
                        {
                            string fileName = Path.GetFileName(new Uri(assetToDownload.webPath).LocalPath);
                            await DownloadImageAsync(assetToDownload.webPath, Environment.CurrentDirectory + "\\Assets\\Temp\\" + fileName);
                            if (File.Exists(Environment.CurrentDirectory + "\\Assets\\Temp\\" + fileName))
                            {
                                IsDownloadInProgress = false;
                                Asset a = assetToDownload.asset;
                                a.Path = fileName;
                                assets.Add(a);
                                assetManager.Add(a);
                                assetToDownloadsPreview.Remove(assetToDownload);
                            }
                        }
                    }
                    catch { }
                }

                if (assetToDownloadsFull.Count > 0)
                {
                    if (!Directory.Exists(Environment.CurrentDirectory + "\\Assets\\Temp"))
                        Directory.CreateDirectory(Environment.CurrentDirectory + "\\Assets\\Temp");
                }

                if (assetZipToDownload.Count > 0 && !IsZipDownloadInProgress)
                {
                    if (!Directory.Exists(Environment.CurrentDirectory + "\\Assets\\Temp"))
                        Directory.CreateDirectory(Environment.CurrentDirectory + "\\Assets\\Temp");

                    AssetToDownload assetToDownload = assetZipToDownload[0];
                    IsZipDownloadInProgress = true;
                    tryingToDownload = false;

                    await DownloadFileAsync(assetToDownload.fullWebPath, assetToDownload.outputDirectory + "\\" + assetToDownload.fileName, p =>
                    {
                        // This action runs on the background thread and simply updates the progress
                        zipProgress = p;
                    }).ContinueWith(t =>
                    {
                        // Handle any exceptions
                        if (t.Exception != null)
                            throw new Exception("Error?");
                    });

                    if (File.Exists(assetToDownload.outputDirectory + "\\" + assetToDownload.fileName))
                    {
                        var dirName = assetToDownload.outputDirectory + "\\" + Path.GetFileNameWithoutExtension(assetToDownload.fileName);
                        Directory.CreateDirectory(dirName);
                        ZipFile.ExtractToDirectory(assetToDownload.outputDirectory + "\\" + assetToDownload.fileName,
                                                   dirName);

                        //File.Delete(assetToDownload.outputDirectory + "\\" + assetToDownload.fileName);

                        List<string> imagePaths = ExtractImagePaths(dirName);
                        string assetFolder = Environment.CurrentDirectory + "\\Assets\\Textures\\" + Path.GetFileNameWithoutExtension(assetToDownload.fileName);
                        if (!Directory.Exists(assetFolder))
                            Directory.CreateDirectory(assetFolder);
                        foreach (string imagePath in imagePaths)
                        {
                            try
                            {
                                File.Copy(imagePath, assetFolder + "\\" + Path.GetFileName(imagePath));
                            }
                            catch { }
                        }

                        try
                        {
                            // Deleting temp folder content
                            DirectoryInfo di = new DirectoryInfo(dirName);
                            foreach (FileInfo file in di.GetFiles())
                            {
                                file.Delete();
                            }
                            foreach (DirectoryInfo subDirectory in di.GetDirectories())
                            {
                                subDirectory.Delete(true); // true => delete recursively
                            }

                            Directory.Delete(dirName);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(e.Message);
                            // TODO: console logging
                        }

                        assetZipToDownload.Remove(assetToDownload);
                    }

                    IsZipDownloadInProgress = false;
                    zipProgress = 0.0f;
                }
            }
        }

        private List<string> ExtractImagePaths(string directoryPath)
        {
            List<string> paths = new List<string>();
            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
            SearchDirectory(directoryInfo, ref paths);
            return paths;
        }

        private void SearchDirectory(DirectoryInfo directoryInfo, ref List<string> paths)
        {
            // Get all files in the current directory with specified extensions
            foreach (FileInfo fileInfo in directoryInfo.GetFiles())
            {
                if (fileInfo.Extension.Equals(".png", System.StringComparison.OrdinalIgnoreCase) ||
                    fileInfo.Extension.Equals(".jpg", System.StringComparison.OrdinalIgnoreCase) ||
                    fileInfo.Extension.Equals(".jpeg", System.StringComparison.OrdinalIgnoreCase) ||
                    fileInfo.Extension.Equals(".bmp", System.StringComparison.OrdinalIgnoreCase))
                {
                    paths.Add(fileInfo.FullName);
                }
            }

            // Recursively search subdirectories
            foreach (DirectoryInfo subDirectoryInfo in directoryInfo.GetDirectories())
            {
                SearchDirectory(subDirectoryInfo, ref paths);
            }
        }

        private async Task DownloadImageAsync(string imageUrl, string localFilePath)
        {
            IsDownloadInProgress = true;

            await Task.Run(async () =>
            {
                try
                {
                    HttpResponseMessage response = await _httpClient.GetAsync(imageUrl);
                    response.EnsureSuccessStatusCode();

                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                    await File.WriteAllBytesAsync(localFilePath, imageBytes);
                }
                catch (Exception e)
                {
                    throw new Exception("Error occurred: " + e.Message);
                }
            });
        }

        public void DeleteFolderContent(string folder)
        {
            try
            {
                // Deleting temp folder content
                DirectoryInfo di = new DirectoryInfo(Environment.CurrentDirectory + "\\Assets\\" + folder);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo subDirectory in di.GetDirectories())
                {
                    subDirectory.Delete(true); // true => delete recursively
                }
            }
            catch(Exception e)
            {
                throw new Exception(e.Message);
                // TODO: console logging
            }
        }

        private bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 1000); // Timeout set to 1000ms
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                // Handle any exceptions (e.g., no network adapters, firewalls blocking the ping, etc.)
                return false;
            }
        }
    }
}
