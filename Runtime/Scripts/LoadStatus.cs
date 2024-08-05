using UnityEngine.ResourceManagement.AsyncOperations;

namespace HybridCLRIntegration
{
    public struct LoadStatus
    {
        public readonly LoadPhase loadPhase;
        
        public long totalBytes => this._downloadStatus?.TotalBytes ?? 0;
        public long downloadedBytes => this._downloadStatus?.DownloadedBytes ?? 0;
        public bool isDownloadDone => this._downloadStatus?.IsDone ?? false;
        
        public float downloadedPercent => this._downloadStatus?.Percent ?? 0;
        
        public readonly float percentComplete;

        private readonly DownloadStatus? _downloadStatus;
        
        public LoadStatus(LoadPhase loadPhase, DownloadStatus? downloadStatus = null, float? percentComplete = null)
        {
            this.loadPhase = loadPhase;
            this._downloadStatus = downloadStatus;
            this.percentComplete = percentComplete ?? 0;
        }
    }
    
    public enum LoadPhase
    {
        Loading,
        WaitingRetry,
        Failed
    }
}