using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace DropBox
{
    class Program
    {
        private static string _path = String.Format(@"{0}\MyDocs", 
            Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory));

        private static CloudBlobContainer _container;

        [STAThread]
        static void Main(string[] args)
        {
            SetupBlobStorageAccess();
            SetupFileSynchronization();

            string consoleInput = string.Empty;
            while (!consoleInput.ToLower().Equals("x"))
            {
                Console.WriteLine("Contents of cloud container '{0}\nSelect blob to share:", _container.Name);
                var blobs = _container.ListBlobs().ToArray<IListBlobItem>();
                for (int i = 0; i < blobs.Length; i++)
                {
                    Console.WriteLine("[{0}]: {1}", i, blobs[i].Uri);
                }
                Console.WriteLine("[r] to refresh or [x] to exit: ");
                consoleInput = Console.ReadLine();

                int n = -1;
                if (int.TryParse(consoleInput, out n) && (n >= 0 && n < blobs.Length))
                {
                    var policy = new SharedAccessBlobPolicy()
                    {
                        Permissions = SharedAccessBlobPermissions.Read,
                        SharedAccessExpiryTime = DateTime.UtcNow + TimeSpan.FromMinutes(5)
                    };

                    var blob = blobs[n] as CloudBlockBlob;
                    string sas = blob.GetSharedAccessSignature(policy);
                    string sasUri = string.Format("{0}{1}", blob.Uri, sas);

                    Console.WriteLine(sasUri);

                    System.Windows.Clipboard.SetText(sasUri);
                }
            }
        }

        private static void SetupFileSynchronization()
        {
            FileSystemEventHandler onFileCreatedOrChanged = (object sender, FileSystemEventArgs args) =>
            {
                var blob = _container.GetBlockBlobReference(args.Name);
                Console.WriteLine("Uploading '{0}'", args.Name);
                blob.UploadFromFileAsync(args.FullPath, FileMode.Open);
            };

            FileSystemEventHandler onFileDeleted = (object sender, FileSystemEventArgs args) =>
            {
                var blob = _container.GetBlockBlobReference(args.Name);
                Console.WriteLine("Deleting '{0}'", args.Name);
                blob.DeleteIfExists();
            };

            var watcher = new System.IO.FileSystemWatcher(_path);
            watcher.Created += onFileCreatedOrChanged;
            watcher.Changed += onFileCreatedOrChanged;
            watcher.Deleted += onFileDeleted;
            watcher.EnableRaisingEvents = true;


        }

        private static void SetupBlobStorageAccess()
        {
            var accountName = "cesdevsto";
            var accountKey = @"4W1ze3igGgmrLqpVE3WVS/shD4+dHMD4LDG/H1YirkNjf+BKsw8CW2XrWJdfeB1kVfoqr8cYoFPxB4NW8u1kow==";
            var credentials = new StorageCredentials(accountName, accountKey);

            CloudStorageAccount azurageStorageAccount = new CloudStorageAccount(credentials, true);
            var blobClient = azurageStorageAccount.CreateCloudBlobClient();
            _container = blobClient.GetContainerReference("mydocs");
            _container.CreateIfNotExists();


        }
    }
}
