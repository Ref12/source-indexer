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

            for (int i = 0; i < Inputs.Length; i++)
            {
                var input = Inputs[i];
                bool selected = (i % TotalGroups) == (GroupNumber - 1);
                var output = new TaskItem(input);

                output.SetMetadata("IsSelected", selected ? "true" : "false");
                Outputs[i] = output;
                Log.LogMessage(MessageImportance.High, $"{input.ItemSpec} (IsSelected: {selected})");
            }
        }
    }
}
