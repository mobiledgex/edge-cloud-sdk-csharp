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

VSPROJECTROOT := MatchingEngineSDK
APPCLIENTOUT := $(VSPROJECTROOT)/MatchingEngineGrpcLibrary
BINOUT := $(APPCLIENTOUT)/bin
PROTO_INCLUDE_DIRECTORIES := $(DME_PROTO_PATH) $(THIRD_PARTY_PROTOS_PATH)/googleapis

PROTO_INCLUDE_FLAGS += $(addprefix --proto_path ,$(PROTO_INCLUDE_DIRECTORIES))

# Host OS platform tools:
MACOS_PLATFORM := macosx_x64
LINUX_PLATFORM := linux_x64
PLATFORM := $(MACOS_PLATFORM)

GETOUTPUT := $(shell mkdir -p $(TEMP_DIR) && cd $(TEMP_DIR) && curl -sL $(CURL_URL) > tmp.zip; unzip tmp.zip && cd .. && cp -r tmp/tools . && rm -rf tmp && cd ../..)
OUTPUT := $(shell chmod 750 packages/$(GRPCTOOLS_VERSION)/tools/$(PLATFORM)/protoc)
OUTPUT := $(shell chmod 750 packages/$(GRPCTOOLS_VERSION)/tools/$(PLATFORM)/grpc_csharp_plugin)

SOLUTIONLOG := /tmp/$(VSPROJECTROOT)_out.log
PUBLISHLOG := /tmp/nuget_out.log

NUGET_RELEASE_SERVER := https://artifactory.mobiledgex.net/artifactory/nuget-releases/

all: packageRestore build-proto build-all

packageRestore:
	cd $(VSPROJECTROOT) && dotnet restore

build-all: build-proto build-debug build-release

build-proto:
	@echo Absolute Path for Proto files are: $(ABSOLUTEPATH)
	mkdir -p $(APPCLIENTOUT)
	@echo Generating GRPC source code...
	$(PROTOC) $(PROTO_INCLUDE_FLAGS) --csharp_out $(APPCLIENTOUT) --grpc_out $(APPCLIENTOUT) $(DME_PROTO_PATH)/*.proto --plugin=protoc-gen-grpc=packages/$(GRPCTOOLS_VERSION)/tools/$(PLATFORM)/grpc_csharp_plugin
	mkdir -p $(APPCLIENTOUT)/google/api
	$(PROTOC) $(PROTO_INCLUDE_FLAGS) --csharp_out $(APPCLIENTOUT)/google/api --grpc_out $(APPCLIENTOUT) $(THIRD_PARTY_PROTOS_PATH)/googleapis/google/api/*.proto --plugin=protoc-gen-grpc=packages/$(GRPCTOOLS_VERSION)/tools/$(PLATFORM)/grpc_csharp_plugin

build-debug:
	@echo Making Debug Library...
	$(shell MSBuild $(VSPROJECTROOT)/MatchingEngineSDK.sln /p:Configuration=Debug > $(SOLUTIONLOG))
	# Print unbounded log to screen:
	cat $(SOLUTIONLOG)

build-release:
	@echo Making Release Library...
	$(shell MSBuild $(VSPROJECTROOT)/MatchingEngineSDK.sln /p:Configuration=Release > $(SOLUTIONLOG))
	# Print unbounded log to screen:
	cat $(SOLUTIONLOG)

publish:
	echo "Publish command will pause for password if you don't set the Artifactory publish key first..."
	$(shell nuget push $(BINOUT)/Release/Mobiledgex.MatchingEngineGrpcLibrary.1.4.8.nupkg -Source $(NUGET_RELEASE_SERVER) | tee $(PUBLISHLOG))
	cat $(PUBLISHLOG)


clean:
	rm -rf $(BINOUT)
