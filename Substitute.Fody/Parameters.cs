using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Substitute
{
    internal struct Parameters : IEquatable<Parameters>
    {
        internal bool? _DoNotChangeSignature;
        internal bool? _KeepBaseMemberSignature;

        /// <summary>
        /// If this option is enabled, the substitution will only be applied to the method bodies,
        /// but not to any signature.
        /// </summary>
        internal bool DoNotChangeSignature => _DoNotChangeSignature.GetValueOrDefault(false);

        /// <summary>
        /// If this option is enabled, the signatures of a member overwriting another member that is not subject to the
        /// substitution, will be kept.
        /// </summary>
        internal bool KeepBaseMemberSignature => _KeepBaseMemberSignature.GetValueOrDefault(false);

        internal static Parameters GetFromConfig(XElement config)
        {
            var result = new Parameters();

            if (config != null)
            {
                var doNotChangeValue = config.Attribute(nameof(DoNotChangeSignature))?.Value;
                if (doNotChangeValue != null)
                    result._DoNotChangeSignature = XmlConvert.ToBoolean(doNotChangeValue);

                var keepBaseMembersValue = config.Attribute(nameof(KeepBaseMemberSignature))?.Value;
                if (keepBaseMembersValue != null)
                    result._KeepBaseMemberSignature = XmlConvert.ToBoolean(keepBaseMembersValue);
            }

            return result;
        }

        internal Parameters Apply(Parameters parameters)
        {
            if (parameters._DoNotChangeSignature.HasValue)
                _DoNotChangeSignature = parameters._DoNotChangeSignature;

            if (parameters._KeepBaseMemberSignature.HasValue)
                _KeepBaseMemberSignature = parameters._KeepBaseMemberSignature;

            return parameters;
        }

        public override bool Equals(object obj)
        {
            return obj is Parameters && Equals((Parameters)obj);
        }

        public bool Equals(Parameters other) =>
            Nullable.Equals(_DoNotChangeSignature, other._DoNotChangeSignature) &&
                Nullable.Equals(_KeepBaseMemberSignature, other._KeepBaseMemberSignature);

        public override int GetHashCode()
        {
            unchecked
            {
                return _DoNotChangeSignature.GetHashCode() * 23 + _KeepBaseMemberSignature.GetHashCode();
            }
        }

        public static bool operator ==(Parameters parameters1, Parameters parameters2)
        {
            return parameters1.Equals(parameters2);
        }

        public static bool operator !=(Parameters parameters1, Parameters parameters2)
        {
            return !(parameters1 == parameters2);
        }
    }
}
