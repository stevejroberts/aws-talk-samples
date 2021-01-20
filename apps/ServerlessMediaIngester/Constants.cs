namespace MediaIngester
{
    internal abstract class Constants
    {
        // The Amazon Resource Name (ARN) identifier for the step functions state machine,
        // this value is posted to Systems Manager's Parameter Store from the serverless
        // template when the serverless application stack is constructed
        public const string StateMachineArnParamStoreKey = "/mediaingester/statemachine-arn";

        public const string InputsRootPathParameterKey = "/mediaingester/inputs/rootpath";
        public const string OutputsRootPathParameterKey = "/mediaingester/outputs/rootpath";

        // Default minimum confidence level used for label and moderation detection
        public const string MinConfidenceForModerationParameterKey = "/mediaingester/min-moderation-confidence";
        public const string MinConfidenceForKeywordingParameterKey = "/mediaingester/min-keyword-confidence";

        public const string VoiceIdParameterKey = "/mediaingester/outputs/voice-id";
        public const string ThumbnailsMaxDimensionParameterKey = "/mediaingester/outputs/thumbnails-maxdimension";

        public const string PendingJobsTableParameterKey = "/mediaingester/pending-jobs-table";

        public const string AsyncOperationCompletedTopicArnParameterKey = "/mediaingester/notification-arns/asyncoperation-completed";
        public const string IngestCompletedTopicArnParameterKey = "/mediaingester/notification-arns/ingest-completed";

        public const string RekognitionServiceRoleParameterKey = "/mediaingester/roles/rekognition-service-role";

        // 'folder'paths for the sorted and processed outputs
        public const string ImagesOutputSubPath = "images";
        public const string ImageThumbnailsOutputSubPath = ImagesOutputSubPath + "/thumbs";
        public const string VideosOutputSubPath = "videos";
        public const string ConvertedAudioOutputSubPath = "audio-from-text";
        public const string ConvertedTextOutputSubPath = "text-from-audio";

        public const string KeywordsTagKey = "Keywords";
        public const string CelebritiesTagKey = "Celebrities";

        public const string PendingJobsJobIdProperty = "JobId";
        public const string PendingJobsWorkflowStateProperty = "WorkflowState";

        public const string JobCompletionMessageJobIdField = "JobId";
        public const string JobCompletionMessageStatusField = "Status";

        public const int MaxKeywordsOrCelebrities = 10;
    }
}
