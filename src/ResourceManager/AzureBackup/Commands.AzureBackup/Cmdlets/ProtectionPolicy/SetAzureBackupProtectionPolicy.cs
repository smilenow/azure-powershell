﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Management.Automation;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using Microsoft.Azure.Management.BackupServices.Models;

namespace Microsoft.Azure.Commands.AzureBackup.Cmdlets
{
    /// <summary>
    /// Update existing protection policy
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "AzureBackupProtectionPolicy", DefaultParameterSetName = NoScheduleParamSet), OutputType(typeof(AzureBackupProtectionPolicy))]
    public class SetAzureBackupProtectionPolicy : AzureBackupPolicyCmdletBase
    {
        protected const string WeeklyScheduleParamSet = "WeeklyScheduleParamSet";
        protected const string DailyScheduleParamSet = "DailyScheduleParamSet";
        protected const string NoScheduleParamSet = "NoScheduleParamSet";

        [Parameter(Position = 1, Mandatory = false, HelpMessage = AzureBackupCmdletHelpMessage.PolicyNewName)]
        [ValidateNotNullOrEmpty]
        public string NewName { get; set; }

        [Parameter(Position = 2, Mandatory = false, HelpMessage = AzureBackupCmdletHelpMessage.BackupType)]
        [ValidateSet("Full", IgnoreCase = true)]
        public string BackupType { get; set; }

        [Parameter(ParameterSetName = DailyScheduleParamSet, Position = 3, Mandatory = false, HelpMessage = AzureBackupCmdletHelpMessage.DailyScheduleType)]
        public SwitchParameter Daily { get; set; }

        [Parameter(ParameterSetName = WeeklyScheduleParamSet, Position = 4, Mandatory = false, HelpMessage = AzureBackupCmdletHelpMessage.WeeklyScheduleType)]
        public SwitchParameter Weekly { get; set; }

        [Parameter(Position = 5, Mandatory = false, HelpMessage = AzureBackupCmdletHelpMessage.ScheduleRunTimes)]
        public DateTime ScheduleRunTimes { get; set; }

        [Parameter(Position = 6, Mandatory = false, HelpMessage = AzureBackupCmdletHelpMessage.RetentionType)]
        [ValidateSet("Days", IgnoreCase = true)]
        public string RetentionType { get; set; }

        [Parameter(Position = 7, Mandatory = false, HelpMessage = AzureBackupCmdletHelpMessage.RententionDuration)]
        public int RetentionDuration { get; set; }

        [Parameter(ParameterSetName = WeeklyScheduleParamSet, Position = 8, Mandatory = false, HelpMessage = AzureBackupCmdletHelpMessage.ScheduleRunDays)]
        [ValidateSet("Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday", IgnoreCase = true)]
        public string[] ScheduleRunDays { get; set; }

        public override void ExecuteCmdlet()
        {
            base.ExecuteCmdlet();

            ExecutionBlock(() =>
            {
                WriteDebug("Making client call");

                AzureBackupProtectionPolicy policyInfo = azureBackupCmdletHelper.GetAzureBackupProtectionPolicyByName(ProtectionPolicy.Name,
                    ProtectionPolicy.ResourceGroupName, ProtectionPolicy.ResourceName, ProtectionPolicy.Location);
                
                FillRemainingValuesForSetPolicyRequest(policyInfo);

                var backupSchedule = azureBackupCmdletHelper.FillBackupSchedule(BackupType, policyInfo.ScheduleType, ScheduleRunTimes,
                   RetentionType, RetentionDuration, policyInfo.ScheduleRunDays.ToArray<string>());

                NewName = (string.IsNullOrEmpty(NewName) ? policyInfo.Name : NewName);
                var updateProtectionPolicyRequest = new UpdateProtectionPolicyRequest();
                updateProtectionPolicyRequest.PolicyName = this.NewName;
                updateProtectionPolicyRequest.Schedule = backupSchedule;

                if (policyInfo != null)
                {
                    var operationId = AzureBackupClient.ProtectionPolicy.UpdateAsync(policyInfo.InstanceId, updateProtectionPolicyRequest, GetCustomRequestHeaders(), CmdletCancellationToken).Result;
                }
                else
                {
                    var exception = new Exception("Protection Policy Not Found with Name" + ProtectionPolicy.Name);
                    var errorRecord = new ErrorRecord(exception, string.Empty, ErrorCategory.InvalidData, null);
                    WriteError(errorRecord);
                    return;
                }

                WriteDebug("Protection Policy successfully updated");

                AzureBackupProtectionPolicy updatedPolicyInfo = azureBackupCmdletHelper.GetAzureBackupProtectionPolicyByName(NewName,
                    ProtectionPolicy.ResourceGroupName, ProtectionPolicy.ResourceName, ProtectionPolicy.Location);
                WriteDebug("Converting response");
                WriteObject(updatedPolicyInfo);

            });
        }

        private void FillRemainingValuesForSetPolicyRequest(AzureBackupProtectionPolicy policy)
        {
            if(string.IsNullOrEmpty(BackupType))
            {
                BackupType = policy.BackupType;
            }

            if (ScheduleRunTimes == null)
            {
                ScheduleRunTimes = policy.ScheduleRunTimes;
            }

            if (string.IsNullOrEmpty(RetentionType))
            {
                RetentionType = policy.RetentionType;
            }

            if (RetentionDuration == 0)
            {
                RetentionDuration = policy.RetentionDuration;
            }

            if (string.IsNullOrEmpty(BackupType))
            {
                BackupType = policy.BackupType;
            }

            if (this.ParameterSetName != NoScheduleParamSet )
            {
                if (ScheduleRunDays != null && ScheduleRunDays.Length > 0)
                {
                    policy.ScheduleType = ScheduleType.Weekly.ToString();
                    policy.ScheduleRunDays = ScheduleRunDays.ToList<string>();
                }
                else
                {
                    policy.ScheduleType = ScheduleType.Daily.ToString();
                    policy.ScheduleRunDays = new List<string>();
                }          
                
            }
        }
    }
}

