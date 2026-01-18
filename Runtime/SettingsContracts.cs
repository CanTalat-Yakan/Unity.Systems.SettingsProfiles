namespace UnityEssentials
{
    public interface ISettingsVersioned
    {
        public int SchemaVersion { get; }
    }

    public interface ISettingsMigrate
    {
        /// <summary>Migrate from old schema to current. Return true if migrated.</summary>
        public bool TryMigrate(int fromVersion);
    }

    public interface ISettingsValidate
    {
        /// <summary>Clamp/normalize values; throw for unrecoverable invalid state.</summary>
        public void Validate();
    }
}