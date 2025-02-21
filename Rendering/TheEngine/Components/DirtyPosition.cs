using TheEngine.ECS;

namespace TheEngine.Components
{
    public struct DirtyPosition : IComponentData
    {
        private byte enabled;

        public DirtyPosition(bool b)
        {
            enabled = b ? (byte)1 : (byte)0;
        }

        public void Enable()
        {
            enabled = 1;
        }

        public void Disable()
        {
            enabled = 0;
        }

        public static implicit operator bool(DirtyPosition d) => d.enabled == 1;
        public static explicit operator DirtyPosition(bool b) => new DirtyPosition(b);
    }
    
    public struct Collider : IComponentData
    {
    }
}