using System;


namespace fwlib
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public enum BlockType
    {
        Grass,
        Stone
    }

    public enum MoveResult
    {
        Moved,
        CannotMoveLivingEntityInTheWay,
        CannotMoveBlocked,
        NotMoved
    }

    public enum EntityType
    {
        Object,
        Player,
        Monster
    }

    public enum AttackResult
    {
        Hit,
        CriticalHit,
        Miss,
        Killed,
        InvalidAttack
    }

    public enum Textures
    {
        Grass = 1,
        Stone = 26,

        Explosion = 5,
        Miss = 6,
        Chest = 17,
        BlueBag = 8,
        HealthPotion = 19,

        Hero = 11,
        Spider = 21,
        Skeleton = 31,
        Ghost = 40,
        Ghast = 41,
        Demon = 51
    }

    [Flags]
    public enum ItemType
    {
        Health,
        Mana,
        Gold,
        Item
    }
}