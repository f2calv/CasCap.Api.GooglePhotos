using CasCap.Messages;
using System.Text.Json;

namespace CasCap.Tests;

/// <summary>Tests that idiomatic model names preserve Google Photos JSON wire contracts.</summary>
[Trait("Category", "Serialization")]
public sealed class ModelSerializationTests
{
    [Fact]
    public void FilterUsesGoogleWireNames()
    {
        var filter = new Filter
        {
            ContentFilter = new ContentFilter
            {
                IncludedContentCategories = [GooglePhotosContentCategoryType.Animals]
            },
            MediaTypeFilter = new MediaTypeFilter
            {
                MediaTypes = [GooglePhotosMediaType.Photo]
            },
            IncludeArchivedMedia = true
        };

                var json = JsonSerializer.Serialize(filter);
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                Assert.Equal("ANIMALS", root.GetProperty("contentFilter").GetProperty("includedContentCategories")[0].GetString());
                Assert.Equal("PHOTO", root.GetProperty("mediaTypeFilter").GetProperty("mediaTypes")[0].GetString());
                Assert.True(root.GetProperty("includeArchivedMedia").GetBoolean());
                Assert.False(root.TryGetProperty(nameof(Filter.ContentFilter), out _));
        }

        [Fact]
        public void MediaItemMapsGoogleWireNames()
        {
                const string json = """
                        {
                            "id": "media-id",
                            "productUrl": "https://example.test/product",
                            "baseUrl": "https://example.test/media",
                            "mimeType": "image/jpeg",
                            "mediaMetadata": {
                                "creationTime": "2026-09-03T08:00:00Z",
                                "width": "1920",
                                "height": "1080",
                                "photo": {
                                    "cameraMake": "Example",
                                    "focalLength": 35.0
                                }
                            },
                            "filename": "photo.jpg"
                        }
                        """;

                var mediaItem = JsonSerializer.Deserialize<MediaItem>(json);

                Assert.NotNull(mediaItem);
                Assert.Equal("media-id", mediaItem.Id);
                Assert.Equal("photo.jpg", mediaItem.Filename);
                Assert.Equal("1920", mediaItem.MediaMetadata.Width);
                Assert.Equal("Example", mediaItem.MediaMetadata.Photo?.CameraMake);
                Assert.True(mediaItem.IsPhoto);
                Assert.False(mediaItem.IsVideo);
        }

        [Fact]
        public void ResponsesMapGoogleWireNames()
        {
                const string json = """
                        {
                            "newMediaItemResults": [
                                {
                                    "uploadToken": "upload-token",
                                    "status": { "code": 0, "status": "OK" },
                                    "mediaItem": {
                                        "id": "media-id",
                                        "mediaMetadata": { "width": "1", "height": "1" },
                                        "filename": "photo.jpg"
                                    }
                                }
                            ]
                        }
                        """;

                var response = JsonSerializer.Deserialize<MediaItemsCreateResponse>(json);

                var result = Assert.Single(response?.NewMediaItemResults ?? []);
                Assert.Equal("upload-token", result.UploadToken);
                Assert.Equal("OK", result.Status.StatusName);
                Assert.Equal("media-id", result.MediaItem.Id);
        }
}
