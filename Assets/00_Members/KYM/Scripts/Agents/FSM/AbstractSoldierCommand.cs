namespace _00_Members.KYM.Scripts.Agents.FSM
{
    public abstract class AbstractSoldierCommand
    {
        protected SoldierActor _owner;
        protected SoldierMover _mover;
        protected SoldierRenderer _renderer;
        
        protected readonly int _stateClipHash;
        
        public AbstractSoldierCommand(SoldierActor owner, int clipHash)
        {
            _owner = owner; 
            _stateClipHash = clipHash;
            _mover = owner.GetModule<SoldierMover>();
            _renderer = owner.GetModule<SoldierRenderer>();
        }

        public virtual void Enter(float transition = 0.2f, int layerIndex = 0)
        {
            
        }
        
        public virtual void Update() {}

        public virtual void Exit() {}
    }
}