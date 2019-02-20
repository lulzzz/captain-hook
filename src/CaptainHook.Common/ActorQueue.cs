using Microsoft.ServiceFabric.Actors.Runtime;

namespace CaptainHook.Common
{
    public class ActorQueue<T>
    {
        internal readonly IActorStateManager StateManager;
        internal int _headIndex = 0;
        internal int _tailIndex = 0;

        public ActorQueue(IActorStateManager stateManager)
        {
            StateManager = stateManager;

            InitializeFromState();
        }

        internal void InitializeFromState()
        {
            throw new System.NotImplementedException();
        }

        public void Enqueue(T item)
        {

        }

        public T Dequeue()
        {
            return default(T);
        }
    }
}
