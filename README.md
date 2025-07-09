# Helika Unity SDK - Developer Guide

A comprehensive Unity SDK for integrating Helika's analytics and telemetry services into Unity games. This repository is open for contributions from Unity game developers who want to help improve and extend the SDK.

## 🚀 Quick Start for Contributors

### Prerequisites
- **Unity**: 2021.3 LTS or later
- **C# Experience**: Intermediate to advanced C# knowledge
- **Git**: Basic Git workflow understanding
- **API Access**: Valid Helika API key and Game ID (contact Helika team)

### Installation Instructions

1. **Clone the Repository**
   ```bash
   git clone https://github.com/helika/helika-unity-sdk.git
   cd helika-unity-sdk
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Click "Open" → "Add" → Select the `helika-unity-sdk` folder
   - Unity will import the project and resolve dependencies

3. **Verify Dependencies**
   - Check that Newtonsoft.Json is available in `Assets/Helika/Packages/Newtonsoft/`
   - Ensure all meta files are properly generated
   - Verify the project opens without errors

4. **Test the Sample Scene**
   - Open `Assets/Scenes/SampleScene.unity`
   - Select the GameObject with `HelikaAssetScript` component
   - Configure your API key and Game ID in the inspector
   - Press Play to test the SDK

## 📁 Project Structure

```
Assets/Helika/
├── Source/                          # Core SDK source code
│   ├── Enums/                      # Enumeration definitions
│   │   ├── HelikaEnvironment.cs    # Environment configuration
│   │   └── TelemetryLevel.cs       # Telemetry level definitions
│   ├── EventManager.cs             # Main event management system
│   └── Singleton.cs                # Singleton base class
├── Editor/                         # Unity Editor integration
│   └── HelikaDependencies         # iOS dependency configuration
├── Packages/                       # Third-party dependencies
│   └── Newtonsoft/                # JSON.NET library
├── HelikaAssetScript.cs           # Example implementation component
└── Scenes/                        # Sample scenes for testing
```

## 🏗️ Architecture Overview

### Core Components

**EventManager** (`Assets/Helika/Source/EventManager.cs`)
- Central event processing and API communication
- Session management and user identification
- Automatic data enrichment and PII collection
- Asynchronous network operations

**Singleton Pattern** (`Assets/Helika/Source/Singleton.cs`)
- Reliable singleton management using ScriptableObjects
- Persists across scene changes and reloads
- Thread-safe instance access

**Configuration Enums**
- `HelikaEnvironment`: API endpoints (Localhost, Develop, Production)
- `TelemetryLevel`: Data collection levels (None, TelemetryOnly, All)

## 🔧 Development Setup

### Local Development Environment

1. **Create a Development Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Configure for Local Testing**
   ```csharp
   // In HelikaAssetScript.cs, set environment to Localhost
   public HelikaEnvironment helikaEnv = HelikaEnvironment.Localhost;
   ```

3. **Enable Debug Logging**
   ```csharp
   // Enable console output for debugging
   public bool printEventsToConsole = true;
   ```

### Testing Your Changes

1. **Unit Testing**
   - Create test scripts in a separate test folder
   - Test individual components in isolation
   - Mock network requests for reliable testing

2. **Integration Testing**
   - Test with different Unity versions
   - Verify functionality across platforms
   - Test error scenarios and edge cases

3. **Manual Testing**
   - Use the sample scene for manual testing
   - Test with different telemetry levels
   - Verify PII data collection

## 📊 Understanding the Data Flow

### Event Processing Pipeline
```
Game Event → EventManager → Validation → Data Enrichment → API Call → Response
```

### Data Enrichment Process
Each event is automatically enriched with:
- **Helika Metadata**: SDK version, platform, taxonomy version
- **Session Data**: Session ID, user ID, timestamps
- **User Details**: User ID, email, wallet (for user events)
- **App Details**: Platform, version, store information
- **PII Data**: Device info, OS details (when enabled)

## 🛠️ Common Development Tasks

### Adding New Event Types

1. **Define Event Structure**
   ```csharp
   JObject newEvent = new JObject(
       new JProperty("event_type", "custom_event"),
       new JProperty("event", new JObject(
           new JProperty("event_sub_type", "custom_action"),
           new JProperty("custom_data", "value")
       ))
   );
   ```

2. **Send the Event**
   ```csharp
   // For user events (includes PII)
   eventManager.SendUserEvent(newEvent);
   
   // For non-user events (no PII)
   eventManager.SendEvent(newEvent);
   ```

### Modifying PII Data Collection

1. **Update PII Collection** (`EventManager.cs` line ~280)
   ```csharp
   private void AppendPIITracking(JObject gameEvent)
   {
       JObject piiData = new JObject(
           // Add your new PII fields here
           new JProperty("custom_device_info", "value")
       );
       // ... existing code
   }
   ```

2. **Add New Telemetry Levels**
   - Update `TelemetryLevel.cs` enum
   - Modify logic in `EventManager.cs` to handle new levels

### Extending API Integration

1. **Add New Endpoints**
   ```csharp
   // In EventManager.cs, add new methods
   public void SendCustomData(string endpoint, JObject data)
   {
       // Implementation
   }
   ```

2. **Modify Authentication**
   - Update headers in `PostAsync` method
   - Add new authentication methods as needed

## 🧪 Testing Guidelines

### Unit Tests
```csharp
[Test]
public void TestEventValidation()
{
    // Test event validation logic
    var invalidEvent = new JObject();
    Assert.Throws<ArgumentException>(() => 
        eventManager.SendEvent(invalidEvent));
}
```

### Integration Tests
```csharp
[Test]
public void TestSessionCreation()
{
    // Test session creation and persistence
    eventManager.Init("test-key", "test-game", HelikaEnvironment.Localhost);
    Assert.IsNotNull(eventManager.GetSessionID());
}
```

## 📝 Code Style Guidelines

### C# Conventions
- Use PascalCase for public methods and properties
- Use camelCase for private fields and local variables
- Include XML documentation for public APIs
- Follow Unity naming conventions

### File Organization
- Keep related functionality in the same file
- Use regions to organize large files
- Separate concerns between different classes

### Error Handling
```csharp
// Always validate inputs
if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new ArgumentException("API key cannot be null or empty");
}

// Use try-catch for network operations
try
{
    // Network operation
}
catch (Exception ex)
{
    Debug.LogError($"Network error: {ex.Message}");
}
```

## 🔄 Building and Distribution

### Building the Unity Package
```bash
# From the project root
<path-to>\Unity.exe -gvh_disable -projectPath <path-to-folder> -exportPackage Assets\Helika Helika.unitypackage
```

### Version Management
1. Update version in `EventManager.cs`:
   ```csharp
   private const string SdkVersion = "0.3.1"; // Update this
   ```

2. Update this README with new version information
3. Create a changelog entry

## 🤝 Contributing Process

### Before You Start
1. **Check Existing Issues**: Look for similar issues or feature requests
2. **Discuss Changes**: Open an issue to discuss major changes
3. **Fork the Repository**: Create your own fork for development

### Development Workflow
1. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make Your Changes**
   - Follow the code style guidelines
   - Add tests for new functionality
   - Update documentation

3. **Test Thoroughly**
   - Run existing tests
   - Add new tests for your changes
   - Test on different Unity versions

4. **Submit Pull Request**
   - Provide clear description of changes
   - Include test results
   - Reference related issues

### Pull Request Checklist
- [ ] Code follows Unity C# conventions
- [ ] No breaking changes without migration guide
- [ ] Tests included for new functionality
- [ ] Documentation updated
- [ ] Error handling implemented
- [ ] Performance impact considered
- [ ] Backward compatibility maintained

## 🐛 Troubleshooting

### Common Development Issues

1. **Unity Import Errors**
   - Delete Library folder and reimport
   - Check for missing meta files
   - Verify Unity version compatibility

2. **Network Testing Issues**
   - Use Localhost environment for testing
   - Check firewall settings
   - Verify API key permissions

3. **Build Errors**
   - Check for missing dependencies
   - Verify platform-specific settings
   - Test on target platforms

### Debug Tools
```csharp
// Enable detailed logging
eventManager.Init(apiKey, gameId, env, telemetry, printEventsToConsole: true);

// Check SDK status
Debug.Log($"SDK Initialized: {eventManager._isInitialized}");
Debug.Log($"Session ID: {eventManager._sessionID}");
```

## 📚 Additional Resources

### Documentation
- [Helika API Documentation](https://helika.notion.site/ELang-Documentation-7fa0071a3f6845abb71aa4b6b41d1353)
- [Unity C# Programming Guide](https://docs.unity3d.com/Manual/CSharpProgramming.html)
- [Newtonsoft.Json Documentation](https://www.newtonsoft.com/json)

### Community
- Join Helika Discord for developer discussions
- Check GitHub Issues for known problems
- Review existing pull requests for examples

---

## 📄 License

This SDK is proprietary software. Please refer to your licensing agreement with Helika for usage terms and conditions.

## 🆘 Support

For technical support and questions:
- Check the troubleshooting section above
- Review existing issues on GitHub
- Contact Helika support team
- Join developer discussions on Discord

---

**Version**: 0.3.0  
**Last Updated**: January 2024  
**Unity Compatibility**: 2021.3 LTS+  
**Contributors Welcome**: Yes! 🎉