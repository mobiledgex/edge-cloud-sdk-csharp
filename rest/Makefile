# Copyright 2018-2020 MobiledgeX, Inc. All rights and licenses reserved.
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

VSPROJECTROOT := MatchingEngineSDKRestLibrary
APPLIBBASE := $(VSPROJECTROOT)
BINOUT := $(APPLIBBASE)/lib

SOLUTIONLOG := /tmp/$(VSPROJECTROOT)_out.log
PUBLISHLOG := /tmp/nuget_out.log

REST_SDK_VERSION=3.0.0
NUGET_RELEASE_SERVER := https://artifactory.mobiledgex.net/api/nuget/nuget-releases/

DOXYGENROOT := Doxygen
DOXYGENLOG := /tmp/doxygen_out.log

REPOROOT := ../

all: packageRestore build-all generate-doxygen

packageRestore:
	dotnet restore

build-all: build-debug build-release

build-debug:
	@echo Making Debug Library...
	$(shell MSBuild $(VSPROJECTROOT)/MatchingEngineSDKRestLibrary.sln /p:Configuration=Debug >> $(SOLUTIONLOG))

build-release:
	@echo Making Release Library...
	$(shell MSBuild $(VSPROJECTROOT)/MatchingEngineSDKRestLibrary.sln /p:Configuration=Release >> $(SOLUTIONLOG))

generate-doxygen:
ifeq (, $(shell which doxygen))
        $(error "Doxygen not installed. Go to https://www.doxygen.nl/download.html to download or run 'brew install doxygen' to install with Homebrew")
endif
	@echo Generating Doxygen documentation...
	$(shell doxygen $(DOXYGENROOT)/Doxyfile > $(DOXYGENLOG) 2>&1)
	$(shell rm -rf $(REPOROOT)/docs/ >> $(DOXYGENLOG))
	$(shell mv html/ $(REPOROOT)/docs/ >> $(DOXYGENLOG))

publish:
	@echo "Publish command will pause for password if you don't set the Artifactory publish key first..."
	$(shell nuget push $(BINOUT)/Release/Mobiledgex.MatchingEngineSDKRestLibrary.$(REST_SDK_VERSION).nupkg -Source $(NUGET_RELEASE_SERVER) >> $(PUBLISHLOG))

clean:
ifneq ("$(BINOUT)", "/lib")
	rm -rf $(BINOUT)
endif
	rm -rf $(APPLIBBASE)/obj

test: build-release
	@echo "Basic test, using the remote publshed library, provided local client depedency isn't set to use the local built file: "
	nuget install -Source MobiledgeX Mobiledgex.MatchingEngineRestSDKLibrary -Version $(REST_SDK_VERSION) -OutputDir packages
	$(shell MSBuild RestSample/RestSample.sln /p:Configuration=Release >> $(SOLUTIONLOG))
	dotnet RestSample/bin/Release/netcoreapp2.1/RestSample.dll
