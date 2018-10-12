using DayCastClient.Properties;
using GalaSoft.MvvmLight.CommandWpf;
using GoogleCast;
using GoogleCast.Channels;
using GoogleCast.Models.Media;
using GoogleCast.Models.Receiver;
using Ookii.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace DayCastClient
{
    public class CastInterface : INotifyPropertyChanged
    {

        #region Private and public properties

        public string LocalIPAddress
        {
            get
            {
                if (string.IsNullOrEmpty(Settings.Default.LocalHostIP))
                {
                    IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (IPAddress ip in host.AddressList)
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                            return ip.ToString();
                    return string.Empty;
                }
                else
                    return Settings.Default.LocalHostIP;
            }
        }

        public string LocalHostPort
        {
            get
            {
                if (string.IsNullOrEmpty(Settings.Default.LocalHostPort))
                    return "5121";
                else
                    return Settings.Default.LocalHostPort;
            }
        }


        private string HostAddress => $"http://{LocalIPAddress}:{LocalHostPort}";

        private ObservableCollection<QueueItem> queue = new ObservableCollection<QueueItem>();
        public ObservableCollection<QueueItem> Queue
        {
            get
            {
                return queue;
            }
            set
            {
                queue = value;
                RaisePropertyChanged(nameof(AreButtonsEnabled));
                RaisePropertyChanged(nameof(Queue));
            }
        }

        public QueueItem SelectedQueueItem { get; set; }

        private List<QueueItem> insertingItems = new List<QueueItem>();

        public ObservableCollection<IReceiver> Receivers { get; set; }

        private static readonly DeviceLocator ReceiverLocator = new DeviceLocator();
        private static readonly Sender MediaSender = new Sender();

        private IReceiver selectedReceiver;
        public IReceiver SelectedReceiver
        {
            get { return selectedReceiver; }
            set
            {
                if (selectedReceiver != null && !selectedReceiver.Equals(value) ||
                    selectedReceiver == null && value != null)
                {
                    selectedReceiver = value;
                    IsInitialized = false;
                    RaisePropertyChanged(nameof(SelectedReceiver));
                    RaisePropertyChanged(nameof(AreButtonsEnabled));
                    RaisePropertyChanged(nameof(IsAddEnabled));
                }
            }
        }

        private string playerState = string.Empty;
        public string PlayerState
        {
            get { return playerState; }
            set { playerState = value; RaisePropertyChanged(nameof(PlayerState)); }
        }

        private bool isInitialized;
        private bool IsInitialized
        {
            get { return isInitialized; }
            set { isInitialized = value; RaisePropertyChanged(nameof(IsInitialized)); }
        }

        private bool isLoaded;
        public bool IsLoaded
        {
            get { return isLoaded; }
            private set
            {
                isLoaded = value;
                RaisePropertyChanged(nameof(IsLoaded));
                RaisePropertyChanged(nameof(AreButtonsEnabled));
                RaisePropertyChanged(nameof(IsAddEnabled));
            }
        }

        public bool AreButtonsEnabled
        {
            get { return IsLoaded && SelectedReceiver != null && !string.IsNullOrWhiteSpace(CurrentTitle); }
        }

        public bool IsAddEnabled
        {
            get { return IsLoaded && SelectedReceiver != null; }
        }

        private bool isMuted;
        public bool IsMuted
        {
            get { return isMuted; }
            set
            {
                if (isMuted != value)
                {
                    isMuted = value;
                    RaisePropertyChanged(nameof(IsMuted));
                }
            }
        }

        public float volume = 1;
        public float Volume
        {
            get { return volume; }
            set
            {
                if (volume != value)
                {
                    if (value > volume)
                    {
                        IsMuted = false;
                    }
                    volume = value;
                    RaisePropertyChanged(nameof(Volume));
                }
            }
        }

        public double currentTime = 0;

        public double CurrentTime
        {
            get { return currentTime; }
            set
            {
                if (currentTime != value)
                {
                    currentTime = value;
                    RaisePropertyChanged(nameof(CurrentTime));
                }
            }
        }

        public double playbackRate = 1;
        public double PlaybackRate
        {
            get { return playbackRate; }
            set
            {
                if (playbackRate != value)
                {
                    playbackRate = value;
                    RaisePropertyChanged(nameof(PlaybackRate));
                }
            }
        }

        public double currentDuration = 0;
        public double CurrentDuration
        {
            get { return currentDuration; }
            set
            {
                if (currentDuration != value)
                {
                    currentDuration = value;
                    RaisePropertyChanged(nameof(CurrentDuration));
                }
            }
        }

        public string currentTitle = string.Empty;
        public string CurrentTitle
        {
            get { return currentTitle; }
            set
            {
                if (currentTitle != value)
                {
                    currentTitle = value;
                    RaisePropertyChanged(nameof(CurrentTitle));
                }
            }
        }

        public int currentOrderId = 0;
        public int CurrentOrderId
        {
            get { return currentOrderId; }
            set
            {
                if (currentOrderId != value)
                {
                    currentOrderId = value;
                    RaisePropertyChanged(nameof(currentOrderId));
                }
            }
        }

        private bool IsStopped
        {
            get
            {
                IMediaChannel mediaChannel = MediaSender.GetChannel<IMediaChannel>();
                return (mediaChannel.Status == null || !String.IsNullOrEmpty(mediaChannel.Status.FirstOrDefault()?.IdleReason));
            }
        }

        public readonly DispatcherTimer SeekTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(1000)
        };

        #endregion

        #region Factory Methods

        public CastInterface()
        {
            DayCastServer.TryLaunchServer(LocalIPAddress, LocalHostPort);
            DayCastServer.ServerQueueReception += DayCastServer_ServerQueueReception;
            DayCastServer.PollServer(HostAddress);

            MediaSender.GetChannel<IMediaChannel>().StatusChanged += MediaChannelStatusChanged;
            MediaSender.GetChannel<IMediaChannel>().QueueStatusChanged += QueueStatusChanged;
            MediaSender.GetChannel<IReceiverChannel>().StatusChanged += ReceiverChannelStatusChanged;
            SeekTimer.Tick += SeekTimer_Tick;

            Queue = new ObservableCollection<QueueItem>();

            InitializeCommands();
        }

        #endregion

        #region Commands

        private void InitializeCommands()
        {
            PlayCommand = new RelayCommand(async () => await PlayAsync(), CanPlay);
            PauseCommand = new RelayCommand(async () => await PauseAsync(), CanPause);
            PlaybackRateCommand = new RelayCommand<object>(async (object playbackRate) => await PlaybackRateAsync(playbackRate), CanPlaybackRate);
            StopCommand = new RelayCommand(async () => await StopAsync(), CanStop);
            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), CanRefresh);
            NextCommand = new RelayCommand(async () => await NextAsync(), CanNext);
            PreviousCommand = new RelayCommand(async () => await PreviousAsync(), CanPrevious);
            SetIsMutedCommand = new RelayCommand(async () => await SetIsMutedAsync(), CanSetIsMuted);
            UpQueueCommand = new RelayCommand<object>(async (object selectedItems) => await UpQueueAsync(selectedItems), CanUpQueue);
            AddCommand = new RelayCommand(() => { }, CanAdd);
            AddFilesCommand = new RelayCommand(async () => await AddFilesAsync(), CanAddFiles);
            AddFolderCommand = new RelayCommand(async () => await AddFolderAsync(), CanAddFolder);
            AddDateFolderCommand = new RelayCommand(async () => await AddDateFolderAsync(), CanAddDateFolder);
            RemoveQueueCommand = new RelayCommand<object>(async (object selectedItems) => await RemoveQueueAsync(selectedItems), CanRemoveQueue);
            DownQueueCommand = new RelayCommand<object>(async (object selectedItems) => await DownQueueAsync(selectedItems), CanDownQueue);
            ShuffleQueueCommand = new RelayCommand(async () => await ShuffleQueueAsync(), CanShuffleQueue);
        }

        public async Task<bool> ConnectAsync()
        {
            IReceiver selectedReceiver = SelectedReceiver;
            if (selectedReceiver != null)
            {
                await MediaSender.ConnectAsync(selectedReceiver);
                return true;
            }
            return false;
        }

        public async Task<bool> TryRefreshAsync()
        {
            try
            {
                await ConnectAsync();
                ISender sender = MediaSender;
                IMediaChannel mediaChannel = sender.GetChannel<IMediaChannel>();
                await mediaChannel.GetStatusAsync();
                await RefreshQueueAsync();

                return true;
            }
            catch { }
            return false;
        }

        public RelayCommand PlayCommand { get; private set; }
        public async Task PlayAsync()
        {
            try
            {
                await SendChannelCommandAsync<IMediaChannel>(!IsInitialized || IsStopped,
                    async c =>
                    {
                        if (insertingItems.Count > 0 && await ConnectAsync())
                        {
                            ISender sender = MediaSender;
                            IMediaChannel mediaChannel = sender.GetChannel<IMediaChannel>();
                            await sender.LaunchAsync(mediaChannel);

                            Queue<QueueItem> itemsQueue = new Queue<QueueItem>(insertingItems);

                            await mediaChannel.QueueLoadAsync(RepeatMode.RepeatOff, itemsQueue.DequeueChunk(20).ToArray());

                            await RefreshQueueAsync();

                            while (itemsQueue.Count > 0)
                                await mediaChannel.QueueInsertAsync(itemsQueue.DequeueChunk(20).ToArray());

                            insertingItems.Clear();

                            IsInitialized = true;
                        }
                    },
                    async c =>
                    {
                        if (insertingItems.Count > 0)
                        {
                            Queue<QueueItem> itemsQueue = new Queue<QueueItem>(insertingItems);

                            while (itemsQueue.Count > 0)
                                await MediaSender.GetChannel<IMediaChannel>().QueueInsertAsync(itemsQueue.DequeueChunk(20).ToArray());

                            insertingItems.Clear();
                        }

                        await c.PlayAsync();
                    });
            }
            catch
            { }
        }
        public bool CanPlay() => AreButtonsEnabled;

        public RelayCommand PauseCommand { get; private set; }
        public async Task PauseAsync()
        {
            await SendChannelCommandAsync<IMediaChannel>(IsStopped, null, async c => await c.PauseAsync());
        }
        public bool CanPause() => AreButtonsEnabled;

        public RelayCommand<object> PlaybackRateCommand { get; private set; }
        public async Task PlaybackRateAsync(object playbackRate)
        {
            if (playbackRate != null && (string)playbackRate != string.Empty)
                await SendChannelCommandAsync<IMediaChannel>(IsStopped, null, async c => await c.SetPlaybackRateMessage(double.Parse((string)playbackRate)));
        }
        public bool CanPlaybackRate(object playbackRate) => AreButtonsEnabled;

        public RelayCommand StopCommand { get; private set; }
        public async Task StopAsync()
        {
            if (IsStopped)
            {
                if (IsInitialized || await ConnectAsync())
                {
                    await InvokeAsync<IReceiverChannel>(c => c.StopAsync());
                }
            }
            else
            {
                await InvokeAsync<IMediaChannel>(c => c.StopAsync());
            }
        }
        public bool CanStop() => AreButtonsEnabled;

        public RelayCommand RefreshCommand { get; private set; }
        public async Task RefreshAsync()
        {
            IsLoaded = false;
            Receivers = new ObservableCollection<IReceiver>(await ReceiverLocator.FindReceiversAsync());
            IsLoaded = true;
            RaisePropertyChanged(nameof(Receivers));
        }
        public bool CanRefresh() => true;

        public RelayCommand SetVolumeCommand { get; private set; }
        public async Task SetVolumeAsync()
        {
            await SendChannelCommandAsync<IReceiverChannel>(IsStopped, null, async c => await c.SetVolumeAsync(Volume));
        }
        public bool CanSetVolume() => AreButtonsEnabled;

        public RelayCommand SetIsMutedCommand { get; private set; }
        private async Task SetIsMutedAsync()
        {
            await SendChannelCommandAsync<IReceiverChannel>(IsStopped, null, async c =>
            {
                IsMuted = !IsMuted;
                await c.SetIsMutedAsync(IsMuted);
            });
        }
        public bool CanSetIsMuted() => AreButtonsEnabled;

        public RelayCommand SeekCommand { get; private set; }
        public async Task SeekAsync()
        {
            await SendChannelCommandAsync<IMediaChannel>(IsStopped, null, async c => await c.SeekAsync(CurrentTime));
        }
        public bool CanSeek() => AreButtonsEnabled;

        public RelayCommand NextCommand { get; private set; }
        private async Task NextAsync()
        {
            await SendChannelCommandAsync<IMediaChannel>(IsStopped, null, async c => await c.NextAsync());
        }
        public bool CanNext() => AreButtonsEnabled;

        public RelayCommand PreviousCommand { get; private set; }
        private async Task PreviousAsync()
        {
            await SendChannelCommandAsync<IMediaChannel>(IsStopped, null, async c => await c.PreviousAsync());
        }
        public bool CanPrevious() => AreButtonsEnabled;

        public RelayCommand<object> UpQueueCommand { get; private set; }
        private async Task UpQueueAsync(object SelectedItems)
        {
            ObservableCollection<int> ids = new ObservableCollection<int>(Queue.Select(i => (int)i.ItemId));
            foreach (QueueItem selectedItem in ((IList)SelectedItems).Cast<QueueItem>().OrderBy(i => i.OrderId))
            {
                int currentIndex = ids.IndexOf((int)selectedItem.ItemId);
                if (currentIndex > 0)
                    ids.Move(currentIndex, currentIndex-1);
            }

            await SendChannelCommandAsync<IMediaChannel>(IsStopped, null, async c => await c.QueueReorderAsync(ids.ToArray()));
        }
        public bool CanUpQueue(object SelectedItems) => ((IList)SelectedItems).Count > 0;

        public RelayCommand<object> DownQueueCommand { get; private set; }
        private async Task DownQueueAsync(object SelectedItems)
        {
            ObservableCollection<int> ids = new ObservableCollection<int>(Queue.Select(i => (int)i.ItemId));
            foreach (QueueItem selectedItem in ((IList)SelectedItems).Cast<QueueItem>().OrderByDescending(i => i.OrderId))
            {
                int currentIndex = ids.IndexOf((int)selectedItem.ItemId);
                if (currentIndex > 0)
                    ids.Move(currentIndex, currentIndex + 1);
            }

            await SendChannelCommandAsync<IMediaChannel>(IsStopped, null, async c => await c.QueueReorderAsync(ids.ToArray()));
        }
        public bool CanDownQueue(object SelectedItems) => ((IList)SelectedItems).Count > 0;


        public RelayCommand AddCommand { get; private set; }
        public bool CanAdd() => IsAddEnabled;
        public RelayCommand AddFilesCommand { get; private set; }
        private async Task AddFilesAsync()
        {
            VistaOpenFileDialog fileBrowserDialog = new VistaOpenFileDialog
            {
                ValidateNames = true,
                Multiselect = true,
                CheckFileExists = true,
                Filter = "MP4|*.mp4"
            };

            if (fileBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                foreach(string path in fileBrowserDialog.FileNames)
                {
                    FileInfo currentFile = new FileInfo(path);

                    if (currentFile.Exists)
                    {
                        if (currentFile.Extension == ".mp4")
                            insertingItems.Add(FileToQueueItem(currentFile));
                    }
                }
                
                await PlayAsync();
            }
        }
        public bool CanAddFiles() => true;

        public RelayCommand AddFolderCommand { get; private set; }
        private async Task AddFolderAsync()
        {
            VistaFolderBrowserDialog fileBrowserDialog = new VistaFolderBrowserDialog
            {
                ShowNewFolderButton = true                
            };

            if (fileBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                DirectoryInfo currentDirectory = new DirectoryInfo(fileBrowserDialog.SelectedPath);

                if (currentDirectory.Exists)
                {
                    foreach (FileInfo file in currentDirectory
                        .EnumerateFileSystemInfos("*.mp4", SearchOption.AllDirectories)
                        .OrderBy(f => f.LastWriteTime))
                    {
                        insertingItems.Add(FileToQueueItem(file));
                    }
                }

                await PlayAsync();
            }
        }
        public bool CanAddFolder() => true;
        
        public RelayCommand AddDateFolderCommand { get; private set; }
        private async Task AddDateFolderAsync()
        {
            VistaFolderBrowserDialog fileBrowserDialog = new VistaFolderBrowserDialog
            {
                ShowNewFolderButton = true
            };

            if (fileBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                DirectoryInfo currentDirectory = new DirectoryInfo(fileBrowserDialog.SelectedPath);

                if (currentDirectory.Exists)
                {
                    DateTime minimumDate = DateTime.Today;
                    
                    foreach (FileInfo file in currentDirectory
                        .EnumerateFileSystemInfos("*.mp4", SearchOption.AllDirectories)
                        .Where(f => f.LastWriteTime > minimumDate)
                        .OrderBy(f => f.LastWriteTime))
                    {
                        insertingItems.Add(FileToQueueItem(file));
                    }
                }

                await PlayAsync();
            }
        }
        public bool CanAddDateFolder() => true;

        public RelayCommand<object> RemoveQueueCommand { get; private set; }
        private async Task RemoveQueueAsync(object SelectedItems)
        {
            await SendChannelCommandAsync<IMediaChannel>(IsStopped, null, async c =>
                await c.QueueRemoveAsync(((IList)SelectedItems).Cast<QueueItem>().Select(q => (int)q.ItemId).ToArray()));
        }
        public bool CanRemoveQueue(object SelectedItems) => ((IList)SelectedItems).Count > 0;

        public RelayCommand ShuffleQueueCommand { get; private set; }
        private async Task ShuffleQueueAsync()
        {
            await SendChannelCommandAsync<IMediaChannel>(IsStopped, null, async c => await c.QueueUpdateAsync(null, true));
        }
        public bool CanShuffleQueue() => AreButtonsEnabled;

        public async Task ChangeCurrentMediaAsync()
        {
            await SendChannelCommandAsync<IMediaChannel>(IsStopped, null, async c => await c.QueueUpdateAsync(SelectedQueueItem.ItemId));
        }

        #endregion

        #region Helper Methods

        private async Task Try(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                PlayerState = ex.GetBaseException().Message;
                IsInitialized = false;
            }
        }

        private async Task InvokeAsync<TChannel>(Func<TChannel, Task> action) where TChannel : IChannel
        {
            if (action != null)
            {
                await action.Invoke(MediaSender.GetChannel<TChannel>());
            }
        }

        private async Task SendChannelCommandAsync<TChannel>(bool condition, Func<TChannel, Task> action, Func<TChannel, Task> otherwise) where TChannel : IChannel
        {
            await InvokeAsync(condition ? action : otherwise);
        }
                     
        #endregion

        #region Event Handlers

        private void MediaChannelStatusChanged(object sender, EventArgs e)
        {
            MediaStatus status = ((IMediaChannel)sender).Status?.FirstOrDefault();
            string playerState = status?.PlayerState;

            if (new string[] { "PLAYING", "BUFFERING", "PAUSED" }.Contains(playerState))
                IsInitialized = true;
            else if (new string[] { "CANCELLED", "FINISHED", "ERROR" }.Contains(status?.IdleReason))
            {
                IsInitialized = false;
                Queue = new ObservableCollection<QueueItem>();
                CurrentDuration = 0;
                CurrentOrderId = 0;
                CurrentTitle = string.Empty;
            }

            if (playerState == "IDLE" && !String.IsNullOrEmpty(status.IdleReason))
                playerState = status.IdleReason;

            PlayerState = playerState;

            if (PlayerState == "PLAYING")
                SeekTimer.Start();
            else
                SeekTimer.Stop();
            
            if (status?.CurrentTime != null)
                CurrentTime = status.CurrentTime;
            
            if (status?.PlaybackRate != null)
                PlaybackRate = (double)status.PlaybackRate;

            if (status?.Media?.Metadata?.Title != null)
                CurrentTitle = status?.Media?.Metadata?.Title;

            if (status?.Media?.Duration != null)
                CurrentDuration = (double)status?.Media?.Duration;

            QueueItem currentItem = Queue.FirstOrDefault(i => i.ItemId == status?.CurrentItemId);

            if (currentItem != null && status?.Media?.Duration != null)
            {
                IList<QueueItem> currentQueue = Queue.ToList();
                currentQueue[currentQueue.IndexOf(currentItem)].Media.Duration = status?.Media?.Duration;
                Queue = new ObservableCollection<QueueItem>(currentQueue);
            }

            if (currentItem?.OrderId != null)
                CurrentOrderId = (int)currentItem?.OrderId;
        }

        private async void QueueStatusChanged(object sender, EventArgs e)
        {
            QueueStatus status = ((IMediaChannel)sender).QueueStatus;

            switch (status.ChangeType)
            {
                case QueueChangeType.Insert:
                    await RefreshQueueAsync(status.ItemIds);
                    break;
                case QueueChangeType.Update:
                    Queue = new ObservableCollection<QueueItem>(Queue.OrderBy(i => Array.IndexOf(status.ItemIds, i.ItemId)));
                    break;
                case QueueChangeType.Remove:
                    IList<QueueItem> currentQueue = Queue.ToList();
                    foreach (int itemId in status.ItemIds)
                        currentQueue.Remove(currentQueue.FirstOrDefault(i => i.ItemId == itemId));
                    Queue = new ObservableCollection<QueueItem>(currentQueue);
                    break;
            }

            RaisePropertyChanged(nameof(AreButtonsEnabled));
        }

        private void ReceiverChannelStatusChanged(object sender, EventArgs e)
        {
            if (!IsInitialized)
            {
                ReceiverStatus status = ((IReceiverChannel)sender).Status;
                if (status != null)
                {
                    if (status.Volume.Level != null)
                    {
                        Volume = (float)status.Volume.Level;
                    }
                    if (status.Volume.IsMuted != null)
                    {
                        IsMuted = (bool)status.Volume.IsMuted;
                    }
                }
            }
        }

        private async void DayCastServer_ServerQueueReception(object sender, DayCastServer.ServerQueueReceptionEventArgs e)
        {
            foreach (string itemPath in e.ReceivedQueueItemPaths)
            {
                FileInfo file = new FileInfo(itemPath);
                if (file.Exists)
                    insertingItems.Add(FileToQueueItem(file));
            }
            
            if (insertingItems.Count() > 0)
                await PlayAsync();
        }

        private void SeekTimer_Tick(object sender, EventArgs e)
        {
            CurrentTime += 1;
        }

        #endregion

        #region Helper Methods

        private async Task RefreshQueueAsync(int[] itemIdsToFetch = null)
        {
            IMediaChannel mediaChannel = MediaSender.GetChannel<IMediaChannel>();
             
            int[] itemIds = itemIdsToFetch ?? await mediaChannel?.QueueGetItemIdsMessage();
            if (itemIds != null && itemIds.Count() > 0)
            {
                Queue<int> itemIdsQueue = new Queue<int>(itemIds);
                IList<QueueItem> currentQueue = Queue.ToList();

                while (itemIdsQueue.Count > 0)
                    foreach (QueueItem item in await mediaChannel.QueueGetItemsMessage(itemIdsQueue.DequeueChunk(20).ToArray()))
                        if (currentQueue.FirstOrDefault(i => i.ItemId == item.ItemId) != null)
                            currentQueue[currentQueue.IndexOf(Queue.FirstOrDefault(i => i.ItemId == item.ItemId))] = item;
                        else
                            if (item.OrderId < currentQueue.Count) currentQueue.Insert((int)item.OrderId, item); else currentQueue.Add(item);

                Queue = new ObservableCollection<QueueItem>(currentQueue);
            }            
        }

        private QueueItem FileToQueueItem(FileInfo fileInfo)
        {
            QueueItem returningQueueItem = new QueueItem()
            {
                Media = new MediaInformation()
                {
                    ContentId = $"{HostAddress}/fetch/{Uri.EscapeDataString(fileInfo.FullName)}",
                    Metadata = new MovieMetadata()
                    {
                        Title = Path.GetFileNameWithoutExtension(fileInfo.Name)
                    }
                },
                Autoplay = true,
                PreloadTime = 5
            };

            FileInfo currentSubtitleFile = null;
            string[] subtitleExtensions = new string[] { ".vtt", ".ttml", ".dfxp", ".xml" };
            foreach (string subtitleExtension in subtitleExtensions)
            {
                currentSubtitleFile = new FileInfo(Path.ChangeExtension(fileInfo.FullName, subtitleExtension));
                if (currentSubtitleFile.Exists)
                    break;
            }

            if (currentSubtitleFile.Exists)
            {
                returningQueueItem.Media.Tracks = new Track[]
                {
                    new Track()
                    {
                        TrackId = 1,
                        Language = "en-US",
                        Name = "English",
                        TrackContentId = $"{HostAddress}/fetch/{Uri.EscapeDataString(currentSubtitleFile.FullName)}"
                    }
                };

                returningQueueItem.Media.TextTrackStyle = new TextTrackStyle()
                {
                    BackgroundColor = System.Drawing.Color.Transparent,
                    EdgeColor = System.Drawing.Color.Black,
                    EdgeType = TextTrackEdgeType.DropShadow
                };

                returningQueueItem.ActiveTrackIds = new int[] { 1 };
            }

            return returningQueueItem;
        }

        #endregion

        #region INotifyPropertyChanged Interface Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion

    }
}