# Makefile

VSPROJECTROOT := MatchingEngineSDKRestLibrary
APPCLIENTOUT := $(VSPROJECTROOT)/MatchingEngineSDKRestLibrary
BINOUT := $(APPCLIENTOUT)/lib

SOLUTIONLOG := /tmp/$(VSPROJECTROOT)_out.log
PUBLISHLOG := /tmp/nuget_out.log

NUGET_RELEASE_SERVER := https://artifactory.mobiledgex.net/artifactory/nuget-releases/

all: packageRestore build-all

packageRestore:
	cd $(VSPROJECTROOT) && dotnet restore

build-all: build-debug build-release

build-debug:
	@echo Making Debug Library...
	$(shell MSBuild $(VSPROJECTROOT)/MatchingEngineSDKRestLibrary.sln /p:Configuration=Debug > $(SOLUTIONLOG))
	# Print unbounded log to screen:
	cat $(SOLUTIONLOG)

build-release:
	@echo Making Release Library...
	$(shell MSBuild $(VSPROJECTROOT)/MatchingEngineSDKRestLibrary.sln /p:Configuration=Release > $(SOLUTIONLOG))
	# Print unbounded log to screen:
	cat $(SOLUTIONLOG)

publish:
	@echo "Publish command will pause for password if you don't set the Artifactory publish key first..."
	$(shell nuget push $(BINOUT)/Release/Mobiledgex.MatchingEngineRestLibrary.1.4.8.nupkg -Source $(NUGET_RELEASE_SERVER) > $(PUBLISHLOG))
	cat $(PUBLISHLOG)


clean:
	rm -rf $(BINOUT)
