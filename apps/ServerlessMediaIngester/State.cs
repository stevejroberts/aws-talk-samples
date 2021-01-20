using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace MediaIngester
{
    /// <summary>
    /// The state passed between the step function executions.
    /// </summary>
    public class State
    {
        /// <summary>
        /// The name of the bucket containing the new or updated object that triggered the 
        /// workflow.
        /// </summary>
        public string Bucket { get; set; }

        /// <summary>
        /// The key of the original object in the bucket whose creation or update triggered the
        /// workflow.
        /// </summary>
        public string InputObjectKey { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ContentTypes
        {
            Image,
            Text,
            Audio,
            Video,
            Unknown
        }

        /// <summary>
        /// What object type is our workflow processing, based on file extension determined
        /// by first step.
        /// </summary>
        public ContentTypes ContentType { get; set; } = ContentTypes.Unknown;

        /// <summary>
        /// The file extension we used to determine the content type.
        /// </summary>
        public string Extension { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum PendingScans
        {
            None,
            Moderation,
            Keywording,
            CelebrityDetection
        }

        /// <summary>
        /// Set if we have started an async scan for moderation, keyword
        /// or celebrity detection on a video. The workflow will restart when the
        /// appropriate SNS topic is signalled and the workflow trigger fires to
        /// restart the workflow at the appropriate point.
        /// </summary>
        public PendingScans PendingScanResults { get; set; } = PendingScans.None;

        /// <summary>
        /// The id of the async job we've started on a video. On resumption the
        /// appropriate task will use this to recover the data that was found and
        /// add it to the state before the workflow continues.
        /// </summary>
        public string PendingJobId { get; set; }

        /// <summary>
        /// Set if for an image or video the moderation step detected unsafe content at or
        /// above our minimum confidence level.
        /// </summary>
        public bool IsUnsafe { get; set; }

        /// <summary>
        /// The key of the converted (or in the case of an image, reduced size) output.
        /// </summary>
        public string OutputObjectKey { get; set; }

        /// <summary>
        /// For an image or video file, the set of keywords describing 'things' that
        /// were detected.
        /// </summary>
        public List<string> Keywords { get; set; } = new List<string>();

        /// <summary>
        /// For an image or video object, indicates if someone famous was detected.
        /// </summary>
        public List<string> Celebrities { get; set; } = new List<string>();

    }
}
