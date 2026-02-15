namespace Logistics_Grid.Framework
{
    internal interface IUtilityDomainCache
    {
        bool Dirty { get; set; }

        int PrimaryCount { get; }
        int SecondaryCount { get; }
        string PrimaryLabel { get; }
        string SecondaryLabel { get; }
    }
}
