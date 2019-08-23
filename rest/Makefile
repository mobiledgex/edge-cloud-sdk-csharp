# Makefile

VSPROJECTROOT := MatchingEngineSDKRestLibrary
APPLIBBASE := $(VSPROJECTROOT)/MatchingEngineSDKRestLibrary
BINOUT := $(APPLIBBASE)/lib

SOLUTIONLOG := /tmp/$(VSPROJECTROOT)_out.log
PUBLISHLOG := /tmp/nuget_out.log

REST_SDK_VERSION=1.4.10
NUGET_RELEASE_SERVER := https://artifactory.mobiledgex.net/artifactory/nuget-releases/

all: packageRestore build-all

packageRestore:
	dotnet restore

build-all: build-debug build-release

build-debug:
	@echo Making Debug Library...
	$(shell MSBuild $(VSPROJECTROOT)/MatchingEngineSDKRestLibrary.sln /p:Configuration=Debug >> $(SOLUTIONLOG))

build-release:
	@echo Making Release Library...
	$(shell MSBuild $(VSPROJECTROOT)/MatchingEngineSDKRestLibrary.sln /p:Configuration=Release >> $(SOLUTIONLOG))

publish:
	@echo "Publish command will pause for password if you don't set the Artifactory publish key first..."
	$(shell nuget push $(BINOUT)/Release/Mobiledgex.MatchingEngineRestLibrary.$(REST_SDK_VERSION).nupkg -Source $(NUGET_RELEASE_SERVER) >> $(PUBLISHLOG))

clean:
ifneq ("$(APPLIBBASE)", "/bin")
	rm -rf $(APPLIBBASE)/bin
endif
	rm -rf $(APPLIBBASE)/obj

test: build-release
	@echo "Basic test, using the remote publshed library, provided local client depedency isn't set to use the local built file: "
	nuget install -Source MobiledgeX Mobiledgex.MatchingEngineRestSDKLibrary -Version $(REST_SDK_VERSION) -OutputDir packages
	$(shell MSBuild RestSample/RestSample.sln /p:Configuration=Release >> $(SOLUTIONLOG))
	dotnet RestSample/bin/Release/netcoreapp2.1/RestSample.dll
