﻿using Azure.Identity;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.SourceBrowser.SourceIndexServer.Models
{
    public class AzureBlobFileSystem : IFileSystem
    {
        private readonly BlobContainerClient container;
        private string clientId;

        public AzureBlobFileSystem(string uri)
        {
            container = new BlobContainerClient(new Uri(uri));

            DefaultAzureCredential credential;

            if (string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ARM_CLIENT_ID")))
                clientId = Environment.GetEnvironmentVariable("ARM_CLIENT_ID");

            if (string.IsNullOrEmpty(clientId))
                credential = new DefaultAzureCredential();
            else
                credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId });

            BlobServiceClient blobServiceClient = new(
                new Uri(uri),
                credential);

            container = blobServiceClient.GetBlobContainerClient(uri);
        }

        public bool DirectoryExists(string name)
        {
            return true;
        }

        public IEnumerable<string> ListFiles(string dirName)
        {
            dirName = dirName.ToLowerInvariant();
            dirName = dirName.Replace("\\", "/");
            if (!dirName.EndsWith("/", StringComparison.Ordinal))
            {
                dirName += "/";
            }

            return container.GetBlobsByHierarchy(prefix: dirName)
                .Where(item => item.IsBlob)
                .Select(item => item.Blob.Name)
                .ToList();
        }

        public bool FileExists(string name)
        {
            name = name.ToLowerInvariant();
            BlobClient blob = container.GetBlobClient(name);
            
            return blob.Exists();
        }

        public Stream OpenSequentialReadStream(string name)
        {
            name = name.ToLowerInvariant();
            BlobClient blob = container.GetBlobClient(name);
            return blob.OpenRead();
        }

        public IEnumerable<string> ReadLines(string name)
        {
            name = name.ToLowerInvariant();
            BlobClient blob = container.GetBlobClient(name);
            using Stream stream = blob.OpenRead();
            using StreamReader reader = new (stream);

            while (!reader.EndOfStream)
            {
                yield return reader.ReadLine();
            }
        }
    }
}