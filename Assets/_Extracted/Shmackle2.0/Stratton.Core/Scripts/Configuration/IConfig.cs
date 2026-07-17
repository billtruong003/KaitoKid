using System;

namespace Stratton.Configuration
{
    public interface IConfig
    {
        Action ConfigUpdated { get; set; }
    }
}