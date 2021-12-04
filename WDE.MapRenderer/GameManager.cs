using System.Collections;
using System.Diagnostics;
using Nito.AsyncEx;
using TheEngine;
using TheEngine.Coroutines;
using TheEngine.Entities;
using TheEngine.PhysicsSystem;
using WDE.Common.DBC;
using WDE.Common.MPQ;
using WDE.Common.Services.MessageBox;
using WDE.MapRenderer.Managers;
using WDE.Module.Attributes;
using WDE.MpqReader;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer
{
    [AutoRegister]
    public class GameManager : IGame, IGameContext
    {
        private readonly IMpqService mpqService;
        private readonly IGameView gameView;
        private readonly IMessageBoxService messageBoxService;
        private readonly IDatabaseClientFileOpener databaseClientFileOpener;
        private AsyncMonitor monitor = new AsyncMonitor();
        private Engine engine;
        public event Action? OnInitialized;
        public event Action? OnFailedInitialize;

        public GameManager(IMpqService mpqService, 
            IGameView gameView,
            IMessageBoxService messageBoxService,
            IDatabaseClientFileOpener databaseClientFileOpener)
        {
            this.mpqService = mpqService;
            this.gameView = gameView;
            this.messageBoxService = messageBoxService;
            this.databaseClientFileOpener = databaseClientFileOpener;
            UpdateLoop = new UpdateManager(this);
        }
        
        private bool TryOpenMpq(out IMpqArchive m, out IMpqArchive m2) // we open two archive, once for async loading, second for sync loading
        {
            m = null!;
            m2 = null!;
            try
            {
                m = mpqService.Open();
                m2 = mpqService.Open();
                return true;
            }
            catch (Exception e)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Invalid MPQ")
                    .SetMainInstruction("Couldn't parse game MPQ.")
                    .SetContent(e.Message + "\n\nAre you using modified game files?")
                    .WithButton("Ok", false)
                    .Build());
                m?.Dispose();
                m = null!;
                m2 = null!;
                return false;
            }
        }
        
        public bool Initialize(Engine engine)
        {
            this.engine = engine;
            if (!TryOpenMpq(out mpq, out mpqSync))
            {
                OnFailedInitialize?.Invoke();
                waitForInitialized.SetResult(false);
                waitForInitialized = new();
                return false;
            }
            coroutineManager = new();
            NotificationsCenter = new NotificationsCenter(this);
            TimeManager = new TimeManager(this);
            ScreenSpaceSelector = new ScreenSpaceSelector(this);
            DbcManager = new DbcManager(this, databaseClientFileOpener);
            CurrentMap = DbcManager.MapStore.FirstOrDefault(m => m.Id == 1) ?? Map.Empty;
            LoadingManager = new(this);
            TextureManager = new WoWTextureManager(this);
            MeshManager = new WoWMeshManager(this);
            MdxManager = new MdxManager(this);
            WmoManager = new WmoManager(this);
            WorldManager = new WorldManager(this);
            ChunkManager = new ChunkManager(this);
            CameraManager = new CameraManager(this);
            LightingManager = new LightingManager(this);
            AreaTriggerManager = new AreaTriggerManager(this);
            RaycastSystem = new RaycastSystem(engine);
            ModuleManager = new ModuleManager(this, gameView); // must be last
            
            OnInitialized?.Invoke();
            IsInitialized = true;
            waitForInitialized.SetResult(true);
            return true;
        }

        public void StartCoroutine(IEnumerator coroutine)
        {
            coroutineManager.Start(coroutine);
        }

        private TaskCompletionSource<bool> waitForInitialized = new();
        public Task<bool> WaitForInitialized => waitForInitialized.Task;

        private Material? prevMaterial;
        public void Update(float delta)
        {
            if (!IsInitialized)
            {
                Console.WriteLine("GameManager not initialized (this is quite fatal)");
                return;
            }

            LoadingManager.Update(delta);
            coroutineManager.Step();

            TimeManager.Update(delta);
            WorldManager.Update(delta);
            
            CameraManager.Update(delta);
            LightingManager.Update(delta);
            
            ScreenSpaceSelector.Update(delta);
            UpdateLoop.Update(delta);
            ChunkManager.Update(delta);
            ModuleManager.Update(delta);
        }

        public void Render(float delta)
        {
            if (!IsInitialized)
            {
                Console.WriteLine("GameManager not initialized (this is quite fatal)");
                return;
            }
            ModuleManager.Render();
            LightingManager.Render();
            AreaTriggerManager.Render();
        }

        public void RenderGUI(float delta)
        {
            ModuleManager.RenderGUI();
            NotificationsCenter.RenderGUI(delta);
            ScreenSpaceSelector.Render();
            CameraManager.RenderGUI();
            LoadingManager.RenderGUI();
        }

        public event Action? RequestDispose;
        public event Action<int>? ChangedMap;

        public void SetMap(int mapId)
        {
            if (DbcManager.MapStore.Contains(mapId) && CurrentMap.Id != mapId)
            {
                CurrentMap = DbcManager.MapStore[mapId];
                ChangedMap?.Invoke(mapId);
            }
        }

        public void DoDispose()
        {
            if (IsInitialized)
                RequestDispose?.Invoke();
            Debug.Assert(!IsInitialized);
        }

        public void DisposeGame()
        {
            if (!IsInitialized)
                return;
            waitForInitialized = new();
            IsInitialized = false;
            ModuleManager.Dispose();
            NotificationsCenter.Dispose();
            LightingManager.Dispose();
            AreaTriggerManager.Dispose();
            ChunkManager.Dispose();
            WorldManager.Dispose();
            LoadingManager.Dispose();
            WmoManager.Dispose();
            MdxManager.Dispose();
            TextureManager.Dispose();
            MeshManager.Dispose();
            mpq.Dispose();
            mpqSync.Dispose();
            NotificationsCenter = null!;
            mpq = null!;
            mpqSync = null!;
            coroutineManager = null!;
            TimeManager = null!;
            ScreenSpaceSelector = null!;
            DbcManager = null!;
            CurrentMap = null!;
            TextureManager = null!;
            MeshManager = null!;
            MdxManager = null!;
            WmoManager = null!;
            ChunkManager = null!;
            CameraManager = null!;
            LightingManager = null!;
            AreaTriggerManager = null!;
            WorldManager = null!;
            RaycastSystem = null!;
            ModuleManager = null!;
            LoadingManager = null!;
        }

        public Engine Engine => engine;

        private IMpqArchive mpq, mpqSync;
        private CoroutineManager coroutineManager;
        public NotificationsCenter NotificationsCenter { get; private set; }
        public TimeManager TimeManager { get; private set; }
        public ScreenSpaceSelector ScreenSpaceSelector { get; private set; }
        public WoWMeshManager MeshManager { get; private set; }
        public WoWTextureManager TextureManager { get; private set; }
        public ChunkManager ChunkManager { get; private set; }
        public ModuleManager ModuleManager { get; private set; }
        public MdxManager MdxManager { get; private set; }
        public WmoManager WmoManager { get; private set; }
        public CameraManager CameraManager { get; private set; }
        public RaycastSystem RaycastSystem { get; private set; }
        public DbcManager DbcManager { get; private set; }
        public LightingManager LightingManager { get; private set; }
        public AreaTriggerManager AreaTriggerManager { get; private set; }
        public UpdateManager UpdateLoop { get; private set; }
        public WorldManager WorldManager { get; private set; }
        public Map CurrentMap { get; private set; }
        public LoadingManager LoadingManager { get; private set; }
        public bool IsInitialized { get; private set; }

        public async Task<PooledArray<byte>?> ReadFile(string fileName)
        {
            using var _ = await monitor.EnterAsync();
            var bytes = await Task.Run(() => mpq.ReadFilePool(fileName));
            if (bytes == null)
                Console.WriteLine("File " + fileName + " is unreadable");
            return bytes;
        }

        public byte[]? ReadFileSync(string fileName)
        {
            var bytes = mpqSync.ReadFile(fileName);
            if (bytes == null)
                Console.WriteLine("File " + fileName + " is unreadable");
            return bytes;
        }
        
        public PooledArray<byte>? ReadFileSyncPool(string fileName)
        {
            var bytes = mpqSync.ReadFilePool(fileName);
            if (bytes == null)
                Console.WriteLine("File " + fileName + " is unreadable");
            return bytes;
        }
    }
}