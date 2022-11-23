# Copyright 2018-2021 MobiledgeX, Inc. All rights and licenses reserved.
# MobiledgeX, Inc. 156 2nd Street #408, San Francisco, CA 94105
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

GRPC_SDK_VERSION := 2.26.0
GRPCTOOLS := Grpc.Tools.$(GRPC_SDK_VERSION)
TEMP_DIR := packages/
CURL_URL := https://www.nuget.org/api/v2/package/Grpc.Tools/$(GRPC_SDK_VERSION)
# Run "brew install coreutils" to get "grealpath" for protoc's requirement of absolute paths
ABSOLUTEPATH := $(shell grealpath ../..)
DME_PROTO_PATH = $(ABSOLUTEPATH)/edge-proto/dme
THIRD_PARTY_PROTOS_PATH = $(ABSOLUTEPATH)/edge-proto/third_party
EDGEPROTOGEN_PATH = $(ABSOLUTEPATH)/edge-proto/edgeprotogen

APPLIBBASE := MatchingEngineGrpc
PROTOCOL_OUT:= MatchingEngineGrpc/Protocol
BINOUT := $(APPLIBBASE)/bin
# Google APIs are for the server, but are referenced in the shared client/server proto.
PROTO_INCLUDE_DIRECTORIES := $(DME_PROTO_PATH) $(THIRD_PARTY_PROTOS_PATH)/googleapis $(EDGEPROTOGEN_PATH)

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
PROTOC := $(TEMP_DIR)/$(GRPCTOOLS)/tools/macosx_x64/protoc

SOLUTIONLOG := /tmp/$(APPLIBBASE)_out.log
PUBLISHLOG := /tmp/nuget_out.log

NUGET_RELEASE_SERVER := https://artifactory.mobiledgex.net/artifactory/nuget-releases/

all: get-grpc-tools packageRestore edge-proto-check build-all

# Note that the solution file MAY include GRPC.tools, but we want Makefile control instead to control the proto src directory.
get-grpc-tools:
	nuget install Grpc.Tools -Version $(GRPC_SDK_VERSION) -OutputDirectory $(TEMP_DIR)

packageRestore:
	nuget restore

build-all: build-proto build-debug build-release third-party-licenses

build-proto:
	@echo Absolute Path for Proto files are: $(ABSOLUTEPATH)
	mkdir -p $(APPLIBBASE)
	@echo Generating GRPC source code...
	@echo The following *should* also skip server REST APIs from shared proto.
	$(PROTOC) $(PROTO_INCLUDE_FLAGS) --csharp_out $(PROTOCOL_OUT) --grpc_out=no_server:$(PROTOCOL_OUT) $(EDGEPROTOGEN_PATH)/*.proto --plugin=protoc-gen-grpc=packages/$(GRPCTOOLS)/tools/$(PLATFORM)/grpc_csharp_plugin
	$(PROTOC) $(PROTO_INCLUDE_FLAGS) --csharp_out $(PROTOCOL_OUT) --grpc_out=no_server:$(PROTOCOL_OUT) $(DME_PROTO_PATH)/*.proto --plugin=protoc-gen-grpc=packages/$(GRPCTOOLS)/tools/$(PLATFORM)/grpc_csharp_plugin
	mkdir -p $(PROTOCOL_OUT)/google/api
	$(PROTOC) $(PROTO_INCLUDE_FLAGS) --csharp_out $(PROTOCOL_OUT)/google/api --grpc_out=no_server:$(PROTOCOL_OUT) $(THIRD_PARTY_PROTOS_PATH)/googleapis/google/api/*.proto --plugin=protoc-gen-grpc=packages/$(GRPCTOOLS)/tools/$(PLATFORM)/grpc_csharp_plugin

build-debug:
	@echo Making Debug Library...
	$(shell MSBuild MobiledgeX.MatchingEngineGrpc.sln /p:Configuration=Debug >> $(SOLUTIONLOG))

build-release:
	@echo Making Release Library...
	$(shell MSBuild MobiledgeX.MatchingEngineGrpc.sln /p:Configuration=Release >> $(SOLUTIONLOG))

publish:
	echo "Publish command will pause for password if you don't set the Artifactory publish key first..."
	$(shell nuget push $(BINOUT)/Release/Mobiledgex.MatchingEngineGrpc.$(GRPC_SDK_VERSION).nupkg -Source $(NUGET_RELEASE_SERVER) >> $(PUBLISHLOG))

third-party-licenses:
	# dotnet tool install --global dotnet-project-licenses to install the tool.
	dotnet-project-licenses -i MatchingEngineGrpc --include-transitive > THIRD_PARTY_LICENSES.txt

clean:
ifneq ("$(APPLIBBASE)", "/bin")
	rm -rf $(APPLIBBASE)/bin
endif
	rm -rf $(APPLIBBASE)/obj
	rm -rf $(TEMP_DIR)

edge-proto-check:
ifneq ("$(wildcard $(DME_PROTO_PATH)/app-client.proto)", "")
	@echo "Found a proto in the proto path. Good."
else
	@echo "Sanity check: Missing edge-proto repository file!"
	@echo "Expected edge-proto.git repo clone at DME_PROTO_PATH outside at the same directory level this git repo here: $(DME_PROTO_PATH)"
endif

test: build-release
	cd SampleApp && nuget restore
	@echo "Basic test, using the remote published library, provided local client dependency isn't set to use the local built file: "
	nuget install -Source MobiledgeX Mobiledgex.MatchingEngineGrpc -Version $(GRPC_SDK_VERSION) -OutputDir packages
	$(shell MSBuild SampleApp.sln /p:Configuration=Release >> $(SOLUTIONLOG))
	dotnet SampleApp/bin/Release/netcoreapp2.1/SampleApp.dll
