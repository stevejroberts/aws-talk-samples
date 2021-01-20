using System.Collections.Generic;

using Amazon.Lambda.Core;

namespace MediaIngester.WorkflowStepFunctions
{
    public class DetermineNewMediaTypeTask
    {
        readonly Dictionary<string, State.ContentTypes> _extensionToCategoryMap = new Dictionary<string, State.ContentTypes>
        {
            { "jpg", State.ContentTypes.Image },
            { "jpeg", State.ContentTypes.Image },
            { "png", State.ContentTypes.Image },
            { "gif", State.ContentTypes.Image },

            { "mp3", State.ContentTypes.Audio },
            { "wav", State.ContentTypes.Audio },

            { "mp4", State.ContentTypes.Video },

            { "txt", State.ContentTypes.Text },
        };

        public State FunctionHandler(State state, ILambdaContext context)
        {
            context.Logger.LogLine($"Media ingester workflow started to process {state.Bucket}::/{state.InputObjectKey}");

            state.ContentType = State.ContentTypes.Unknown;

            var ext = System.IO.Path.GetExtension(state.InputObjectKey);
            if (!string.IsNullOrEmpty(ext))
            {
                state.Extension = ext.TrimStart('.');
                if (_extensionToCategoryMap.ContainsKey(state.Extension))
                {
                    state.ContentType = _extensionToCategoryMap[state.Extension];
                }
            }

            return state;
        }
    }
}
