#include <WiFiS3.h>
#include <ArduinoJson.h>
#include <EEPROM.h>
#include <WiFiUDP.h>
#include <ArduinoMDNS.h>

const char* AP_SSID = "SmartHome-TemperatureSensor";

WiFiServer server(80);
WiFiUDP mdnsUDP;
MDNS mdns(mdnsUDP);

// EEPROM addresses
const int SSID_ADDRESS = 0;
const int PASSWORD_ADDRESS = 64;

// Maximum sizes
const int SSID_MAX_LENGTH = 32;
const int PASSWORD_MAX_LENGTH = 64;

// mDNS
IPAddress smartHubIP;
bool smartHubFound = false;
bool mdnsLookupComplete = false;

// ============================================================
// mDNS NAME RESOLUTION CALLBACK
// ============================================================

void mdnsNameFoundCallback(const char* name, IPAddress ip) {

  if (ip == INADDR_NONE) {

    Serial.print("mDNS: Could not resolve ");
    Serial.println(name);

    smartHubFound = false;

  } else {

    Serial.print("mDNS: ");
    Serial.print(name);
    Serial.print(" -> ");
    Serial.println(ip);

    smartHubIP = ip;
    smartHubFound = true;
  }

  mdnsLookupComplete = true;
}

// ============================================================
// SETUP
// ============================================================

void setup() {

  Serial.begin(9600);
  delay(2000);

  char ssid[SSID_MAX_LENGTH + 1];
  char password[PASSWORD_MAX_LENGTH + 1];

  bool credentialsExist = loadCredentials(ssid, password);

  if (credentialsExist) {

    Serial.println("Stored Wi-Fi credentials found.");
    Serial.print("SSID: ");
    Serial.println(ssid);

    Serial.println("Connecting to saved Wi-Fi...");

    if (connectToWiFi(ssid, password)) {

      Serial.println("Successfully connected to Wi-Fi!");

      Serial.print("IP address: ");
      Serial.println(WiFi.localIP());

      // Find the Smart Home Hub
      IPAddress hubIP;

      if (findSmartHub(hubIP)) {

        Serial.println("Smart Home Hub discovered!");

        Serial.print("Hub IP: ");
        Serial.println(hubIP);

      } else {

        Serial.println("Could not discover Smart Home Hub.");
      }

    } else {

      Serial.println("Could not connect to saved Wi-Fi.");
      Serial.println("Starting pairing Access Point...");

      startAccessPoint();
    }

  } else {

    Serial.println("No Wi-Fi credentials stored.");
    Serial.println("Starting pairing Access Point...");

    startAccessPoint();
  }
}

// ============================================================
// LOOP
// ============================================================

void loop() {
  mdns.run();
  WiFiClient client = server.available();

  if (client) {

    Serial.println("Client connected!");

    String request = "";

    // Read HTTP headers
    while (client.connected()) {

      if (client.available()) {

        String line = client.readStringUntil('\n');

        Serial.print("HTTP: ");
        Serial.println(line);

        // Empty line = end of headers
        if (line == "\r") {
          break;
        }

        request += line;
      }
    }

    delay(10);

    // Read HTTP body
    String body = "";

    while (client.available()) {
      body += client.readString();
    }

    Serial.println();
    Serial.println("Request body:");
    Serial.println(body);


    // ========================================================
    // CONFIGURE ENDPOINT
    // ========================================================

    if (request.indexOf("POST /api/configure") >= 0) {

      Serial.println("Configuration request received!");

      JsonDocument doc;

      DeserializationError error = deserializeJson(doc, body);

      if (error) {

        Serial.print("JSON parsing failed: ");
        Serial.println(error.c_str());

        client.println("HTTP/1.1 400 Bad Request");
        client.println("Content-Type: application/json");
        client.println("Connection: close");
        client.println();
        client.println("{\"error\":\"Invalid JSON\"}");

      } else {

        const char* ssid = doc["ssid"];
        const char* password = doc["password"];

        if (ssid == nullptr || password == nullptr) {

          Serial.println("SSID or password missing!");

          client.println("HTTP/1.1 400 Bad Request");
          client.println("Content-Type: application/json");
          client.println("Connection: close");
          client.println();
          client.println("{\"error\":\"SSID or password missing\"}");

        } else {

          Serial.println("Wi-Fi configuration received.");

          Serial.print("SSID: ");
          Serial.println(ssid);

          Serial.println("Password received.");

          // Save credentials
          saveCredentials(ssid, password);

          Serial.println("Wi-Fi credentials saved!");


          // Tell Hub that configuration was successful
          client.println("HTTP/1.1 200 OK");
          client.println("Content-Type: application/json");
          client.println("Connection: close");
          client.println();
          client.println("{\"status\":\"configuration saved\"}");

          delay(100);

          // Close the HTTP connection
          client.stop();

          Serial.println("Client disconnected.");

          // Stop the temporary AP
          Serial.println("Stopping pairing Access Point...");

          WiFi.end();

          delay(1000);


          // Connect to the newly configured Wi-Fi
          Serial.println("Connecting to configured Wi-Fi...");

          if (connectToWiFi(ssid, password)) {

            Serial.println("Successfully connected to Wi-Fi!");

            Serial.print("IP address: ");
            Serial.println(WiFi.localIP());

          } else {

            Serial.println("Failed to connect to configured Wi-Fi.");

            Serial.println("Restarting pairing Access Point...");

            startAccessPoint();
          }

          return;
        }
      }

    } else {

      client.println("HTTP/1.1 404 Not Found");
      client.println("Content-Type: application/json");
      client.println("Connection: close");
      client.println();
      client.println("{\"error\":\"Endpoint not found\"}");
    }

    delay(1);
    client.stop();

    Serial.println("Client disconnected.");
  }
}


// ============================================================
// START PAIRING ACCESS POINT
// ============================================================

void startAccessPoint() {

  int status = WiFi.beginAP(AP_SSID);

  if (status == WL_AP_LISTENING) {

    Serial.println("Access Point started!");

    Serial.print("SSID: ");
    Serial.println(AP_SSID);

    Serial.print("IP address: ");
    Serial.println(WiFi.localIP());

    server.begin();

    Serial.println("HTTP server started!");

  } else {

    Serial.println("Failed to start Access Point.");
  }
}


// ============================================================
// CONNECT TO WI-FI
// ============================================================

bool connectToWiFi(const char* ssid, const char* password) {

  Serial.print("Connecting to: ");
  Serial.println(ssid);

  int status = WiFi.begin(ssid, password);

  if (status != WL_CONNECTED) {

    Serial.print("Wi-Fi connection failed. Status: ");
    Serial.println(status);

    return false;
  }

  Serial.println("Wi-Fi connection established.");
  Serial.println("Waiting for DHCP...");

  // Wait for DHCP to assign an IP address
  unsigned long startTime = millis();

  while (WiFi.localIP() == IPAddress(0, 0, 0, 0)) {

    delay(500);

    Serial.print(".");

    // Timeout after 30 seconds
    if (millis() - startTime > 30000) {

      Serial.println();
      Serial.println("DHCP timeout.");

      return false;
    }
  }

  Serial.println();
  Serial.println("DHCP completed.");

  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

  Serial.print("Subnet mask: ");
  Serial.println(WiFi.subnetMask());

  Serial.print("Gateway: ");
  Serial.println(WiFi.gatewayIP());

  // Initialize mDNS
  if (!mdns.begin(WiFi.localIP(), "temperature-sensor")) {

    Serial.println("Failed to start mDNS.");

  } else {

    Serial.println("mDNS started.");
  }

  return true;
}

// ============================================================
// FIND SMART HOME HUB USING mDNS
// ============================================================
// ============================================================
// FIND SMART HOME HUB
// ============================================================

bool findSmartHub(IPAddress& hubIP) {

  Serial.println("Looking for smarthub.local...");

  int result = WiFi.hostByName("smarthub.local", hubIP);

  if (result == 1) {

    Serial.print("Smart Hub found at: ");
    Serial.println(hubIP);

    return true;

  } else {

    Serial.println("Could not resolve smarthub.local.");

    return false;
  }
}

// ============================================================
// SAVE CREDENTIALS
// ============================================================

void saveCredentials(const char* ssid, const char* password) {

  // Clear old SSID
  for (int i = 0; i < SSID_MAX_LENGTH; i++) {
    EEPROM.write(SSID_ADDRESS + i, 0);
  }

  // Clear old password
  for (int i = 0; i < PASSWORD_MAX_LENGTH; i++) {
    EEPROM.write(PASSWORD_ADDRESS + i, 0);
  }

  // Save SSID
  for (int i = 0; i < SSID_MAX_LENGTH && ssid[i] != '\0'; i++) {
    EEPROM.write(SSID_ADDRESS + i, ssid[i]);
  }

  // Save password
  for (int i = 0; i < PASSWORD_MAX_LENGTH && password[i] != '\0'; i++) {
    EEPROM.write(PASSWORD_ADDRESS + i, password[i]);
  }

  Serial.println("Credentials written to EEPROM.");
}


// ============================================================
// LOAD CREDENTIALS
// ============================================================

bool loadCredentials(char* ssid, char* password) {

  // Read SSID
  for (int i = 0; i < SSID_MAX_LENGTH; i++) {
    ssid[i] = EEPROM.read(SSID_ADDRESS + i);
  }

  ssid[SSID_MAX_LENGTH] = '\0';


  // Read password
  for (int i = 0; i < PASSWORD_MAX_LENGTH; i++) {
    password[i] = EEPROM.read(PASSWORD_ADDRESS + i);
  }

  password[PASSWORD_MAX_LENGTH] = '\0';


  // Check if credentials exist
  if (ssid[0] == '\0' || ssid[0] == 255) {

    return false;
  }

  return true;
}
