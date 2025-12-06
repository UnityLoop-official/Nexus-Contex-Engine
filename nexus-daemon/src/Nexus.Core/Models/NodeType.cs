namespace Nexus.Core.Models;

public enum NodeType
{
    SRV,  // Service
    REPO, // Repository
    CTRL, // Controller
    API,  // External API Client
    DOM,  // Domain Model
    TEST, // Test
    JOB,  // Background Job
    CFG,  // Configuration
    UTIL, // Utility
    UNK   // Unknown
}
