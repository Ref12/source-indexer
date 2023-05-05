using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;

namespace Microsoft.SourceIndexer.Tasks
{
    /// <summary>
    /// Used to split out items into groups based on <see cref="GroupNumber"/>.
    /// This is used for job slicing in Azure DevOps pipeline. Pipeline creates separate parallel jobs per group number and executes them concurrently.
    /// </summary>
    public class SelectGroup : Task
    {
        /// <summary>
        /// 1-based index of the group in total number of groups
        /// </summary>
        [Required]
        public int GroupNumber { get; set; }

        /// <summary>
        /// The name of the repository to select
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The name of the repository to select
        /// </summary>
        public string AllNames { get; set; }

        /// <summary>
        /// Total number of groups
        /// </summary>
        [Required]
        public int TotalGroups { get; set; }

        [Required]
        public ITaskItem[] Inputs { get; set; }

        [Output]
        public ITaskItem[] Outputs { get; set; }

        public override bool Execute()
        {
            try
            {
                ExecuteCore();
                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true);
                return false;
            }
        }

        private void ExecuteCore()
        {
            Outputs = new TaskItem[Inputs.Length];

            AllNames ??= "";
            this.Log.LogMessage(MessageImportance.High, $"All repos: '{string.Join(",", Inputs.Select(i => i.ItemSpec))}'");
            var allNames = new HashSet<string>(AllNames.Split(',') ?? new string[0], StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < Inputs.Length; i++)
            {
                var input = Inputs[i];
                bool selected = IsSelected(i, input, allNames);
                var output = new TaskItem(input);

                output.SetMetadata("IsSelected", selected ? "true" : "false");
                Outputs[i] = output;
                Log.LogMessage(MessageImportance.High, $"{input.ItemSpec} (IsSelected: {selected})");
            }
        }

        private bool IsSelected(int i, ITaskItem input, HashSet<string> allNames)
        {
            if (!allNames.Contains(input.ItemSpec))
            {
                this.Log.LogWarning($"Found unregistered item: {input.ItemSpec}");

                if (string.Equals(Name, "_other", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return string.IsNullOrEmpty(Name)
                                ? (i % TotalGroups) == (GroupNumber - 1)
                                : string.Equals(Name, input.ItemSpec, StringComparison.OrdinalIgnoreCase);
        }
    }
}
