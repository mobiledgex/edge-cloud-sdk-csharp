# Makefile

GRPCTOOLS_VERSION := Grpc.Tools.1.21.0
TEMP_DIR := packages/$(GRPCTOOLS_VERSION)/tmp
CURL_URL := https://www.nuget.org/api/v2/package/Grpc.Tools
#PROTOC := packages/$(GRPCTOOLS_VERSION)/tools/macosx_x64/protoc
PROTOC := protoc
# Run "brew install coreutils" to get "grealpath" for protoc's requirement of absolute paths
ABSOLUTEPATH := $(shell grealpath ../..)
DME_PROTO_PATH = $(ABSOLUTEPATH)/edge-proto/dme
THIRD_PARTY_PROTOS_PATH = $(ABSOLUTEPATH)/edge-proto/third_party

GRPC_SDK_VERSION := 1.4.8
VSPROJECTROOT := MatchingEngineSDK
APPLIBBASE := $(VSPROJECTROOT)/MatchingEngineGrpcLibrary
BINOUT := $(APPLIBBASE)/bin
PROTO_INCLUDE_DIRECTORIES := $(DME_PROTO_PATH) $(THIRD_PARTY_PROTOS_PATH)/googleapis

PROTO_INCLUDE_FLAGS += $(addprefix --proto_path ,$(PROTO_INCLUDE_DIRECTORIES))

# Host OS platform tools:
UNAME_S := $(shell uname -s)
ifeq ($(UNAME_S), Linux)
	PLATFORM := linux_x64
else ifeq ($(UNAME_S), Darwin)
	PLATFORM := macosx_x64
else
	"Unsupported platform"
	exit 1
endif

GETOUTPUT := $(shell mkdir -p $(TEMP_DIR) && cd $(TEMP_DIR) && curl -sL $(CURL_URL) > tmp.zip; unzip tmp.zip && cd .. && cp -r tmp/tools . && rm -rf tmp && cd ../..)
OUTPUT := $(shell chmod 750 packages/$(GRPCTOOLS_VERSION)/tools/$(PLATFORM)/protoc)
OUTPUT := $(shell chmod 750 packages/$(GRPCTOOLS_VERSION)/tools/$(PLATFORM)/grpc_csharp_plugin)

SOLUTIONLOG := /tmp/$(VSPROJECTROOT)_out.log
PUBLISHLOG := /tmp/nuget_out.log

NUGET_RELEASE_SERVER := https://artifactory.mobiledgex.net/artifactory/nuget-releases/

all: packageRestore edge-proto-check build-proto build-all

packageRestore:
	cd $(VSPROJECTROOT) && nuget restore

build-all: build-proto build-debug build-release

build-proto:
	@echo Absolute Path for Proto files are: $(ABSOLUTEPATH)
	mkdir -p $(APPLIBBASE)
	@echo Generating GRPC source code...
	$(PROTOC) $(PROTO_INCLUDE_FLAGS) --csharp_out $(APPLIBBASE) --grpc_out $(APPLIBBASE) $(DME_PROTO_PATH)/*.proto --plugin=protoc-gen-grpc=packages/$(GRPCTOOLS_VERSION)/tools/$(PLATFORM)/grpc_csharp_plugin
	mkdir -p $(APPLIBBASE)/google/api
	$(PROTOC) $(PROTO_INCLUDE_FLAGS) --csharp_out $(APPLIBBASE)/google/api --grpc_out $(APPLIBBASE) $(THIRD_PARTY_PROTOS_PATH)/googleapis/google/api/*.proto --plugin=protoc-gen-grpc=packages/$(GRPCTOOLS_VERSION)/tools/$(PLATFORM)/grpc_csharp_plugin

build-debug:
	@echo Making Debug Library...
	$(shell MSBuild $(VSPROJECTROOT)/MatchingEngineSDK.sln /p:Configuration=Debug >> $(SOLUTIONLOG))

build-release:
	@echo Making Release Library...
	$(shell MSBuild $(VSPROJECTROOT)/MatchingEngineSDK.sln /p:Configuration=Release >> $(SOLUTIONLOG))

publish:
	echo "Publish command will pause for password if you don't set the Artifactory publish key first..."
	$(shell nuget push $(BINOUT)/Release/Mobiledgex.MatchingEngineGrpcLibrary.$(GRPC_SDK_VERSION).nupkg -Source $(NUGET_RELEASE_SERVER) >> $(PUBLISHLOG))


clean:
ifneq ("$(APPLIBBASE)", "/bin")
	rm -rf $(APPLIBBASE)/bin
endif
	rm -rf $(APPLIBBASE)/obj

edge-proto-check:
ifneq ("$(wildcard $(DME_PROTO_PATH)/app-client.proto)", "")
	@echo "Found a proto in the proto path. Good."
else
	@echo "Sanity check: Missing edge-proto repository file!"
	@echo "Expected edge-proto.git repo clone at DME_PROTO_PATH outside at the same directory level this git repo here: $(DME_PROTO_PATH)"
endif

test: build-release
	cd SampleApps && nuget restore
	@echo "Basic test, using the remote publshed library, provided local client depedency isn't set to use the local built file: "
	nuget install -Source MobiledgeX Mobiledgex.MatchingEngineGrpcLibrary -Version $(GRPC_SDK_VERSION) -OutputDir packages
	$(shell MSBuild SampleApps/SampleApps.sln /p:Configuration=Release >> $(SOLUTIONLOG))
	dotnet SampleApps/MeGrpcSampleConsoleApp/bin/Release/netcoreapp2.1/MeGrpcSampleConsoleApp.dll
