﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GoogleCast.Models.Media
{
    /// <summary>
    /// Media status
    /// </summary>
    [DataContract]
    public class MediaStatus
    {
        /// <summary>
        /// Gets or sets the media session identifier
        /// </summary>
        [DataMember(Name = "mediaSessionId")]
        public long MediaSessionId { get; set; }

        /// <summary>
        /// Gets or sets the playback rate
        /// </summary>
        [DataMember(Name = "playbackRate")]
        public double PlaybackRate { get; set; }

        /// <summary>
        /// Gets or sets the player state
        /// </summary>
        [DataMember(Name = "playerState")]
        public string PlayerState { get; set; }

        /// <summary>
        /// Gets or sets the current time
        /// </summary>
        [DataMember(Name = "currentTime")]
        public double CurrentTime { get; set; }

        /// <summary>
        /// Gets or sets the supported media commands
        /// </summary>
        [DataMember(Name = "supportedMediaCommands")]
        public int SupportedMediaCommands { get; set; }

        /// <summary>
        /// Gets or sets the volume
        /// </summary>
        [DataMember(Name = "volume")]
        public Volume Volume { get; set; }

        /// <summary>
        /// Gets or sets the idle reason
        /// </summary>
        [DataMember(Name = "idleReason")]
        public string IdleReason { get; set; }

        /// <summary>
        /// Gets or sets the media
        /// </summary>
        [DataMember(Name = "media")]
        public MediaInformation Media { get; set; }

        /// <summary>
        /// Gets or sets the current item identifier
        /// </summary>
        [DataMember(Name = "currentItemId")]
        public int CurrentItemId { get; set; }

        /// <summary>
        /// Gets or sets the extended status
        /// </summary>
        [DataMember(Name = "extendedStatus")]
        public MediaStatus ExtendedStatus { get; set; }

        /// <summary>
        /// Gets or sets the repeat mode
        /// </summary>
        [DataMember(Name = "repeatMode")]
        public string RepeatMode { get; set; }

        /// <summary>
        /// Gets or sets the queue items
        /// </summary>
        [DataMember(Name = "items")]
        public IEnumerable<QueueItem> QueueItems { get; set; }
    }
}
