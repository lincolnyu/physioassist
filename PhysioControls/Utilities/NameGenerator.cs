using System;
using PhysioControls.EntityDataModel;

namespace PhysioControls.Utilities
{
    public static class NameGenerator
    {
        public static string GetValidName(this Page page, string initialName)
        {
            if (!string.IsNullOrWhiteSpace(initialName) && !page.ContainsDataObjectName(initialName))
            {
                return initialName;
            }

            for (int i = 1; i < int.MaxValue; i++)
            {
                string name = string.Format("DataObject {0}", i);
                if (!page.ContainsDataObjectName(name))
                    return name;
            }

            throw new ApplicationException("Unable to generate a valid name for the new element");
        }
    }
}
