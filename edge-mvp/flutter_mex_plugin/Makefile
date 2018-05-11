# Makefile

PROTO-PATH=../../../edge-cloud/d-match-engine/dme-proto
DART-OUT=lib/

build:
	protoc --dart_out=grpc:${DART-OUT} --proto_path=${PROTO-PATH} app-client.proto
