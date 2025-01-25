using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;

namespace HydraEngine.Protection.CodeEncryption.Stuffs
{
    internal static class AntiTamperExtensions
    {
        #region Reloc
        internal static void AddBeforeReloc(this List<PESection> sections, PESection newSection)
        {
            if (sections == null) throw new ArgumentNullException(nameof(sections));
            InsertBeforeReloc(sections, sections.Count, newSection);
        }
        internal static void InsertBeforeReloc(this List<PESection> sections, int preferredIndex, PESection newSection)
        {
            if (sections == null) throw new ArgumentNullException(nameof(sections));
            if (preferredIndex < 0 || preferredIndex > sections.Count) throw new ArgumentOutOfRangeException(nameof(preferredIndex), preferredIndex, "Preferred index is out of range.");
            if (newSection == null) throw new ArgumentNullException(nameof(newSection));
            var relocIndex = sections.FindIndex(0, Math.Min(preferredIndex + new Random().Next(2, 4), sections.Count), IsRelocSection);
            if (relocIndex == -1)
                sections.Insert(preferredIndex, newSection);
            else
                sections.Insert(relocIndex, newSection);
        }
        private static bool IsRelocSection(PESection section) =>
            section.Name.Equals(".reloc", StringComparison.Ordinal);
        #endregion
    }
    internal static class AntiTamperExtensions2
    {
        #region Reloc
        internal static void AddBeforeRsrc(this List<PESection> sections, PESection newSection)
        {
            if (sections == null) throw new ArgumentNullException(nameof(sections));
            InsertBeforeRsrc(sections, sections.Count, newSection);
        }
        internal static void InsertBeforeRsrc(this List<PESection> sections, int preferredIndex, PESection newSection)
        {
            if (sections == null) throw new ArgumentNullException(nameof(sections));
            if (preferredIndex < 0 || preferredIndex > sections.Count) throw new ArgumentOutOfRangeException(nameof(preferredIndex), preferredIndex, "Preferred index is out of range.");
            if (newSection == null) throw new ArgumentNullException(nameof(newSection));
            var relocIndex = sections.FindIndex(0, Math.Min(preferredIndex + new Random().Next(2, 4), sections.Count), IsRsrcSection);
            if (relocIndex == -1)
                sections.Insert(preferredIndex, newSection);
            else
                sections.Insert(relocIndex, newSection);
        }
        private static bool IsRsrcSection(PESection section) =>
            section.Name.Equals(".rsrc", StringComparison.Ordinal);
        #endregion
    }
}
