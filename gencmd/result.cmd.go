// Code generated by protoc-gen-gogo. DO NOT EDIT.
// source: result.proto

package gencmd

import edgeproto "github.com/mobiledgex/edge-cloud/edgeproto"
import proto "github.com/gogo/protobuf/proto"
import fmt "fmt"
import math "math"
import _ "github.com/gogo/googleapis/google/api"

// Reference imports to suppress errors if they are not otherwise used.
var _ = proto.Marshal
var _ = fmt.Errorf
var _ = math.Inf

// Auto-generated code: DO NOT EDIT
func ResultSlicer(in *edgeproto.Result) []string {
	s := make([]string, 0, 2)
	s = append(s, in.Message)
	s = append(s, string(in.Code))
	return s
}

func ResultHeaderSlicer() []string {
	s := make([]string, 0, 2)
	s = append(s, "Message")
	s = append(s, "Code")
	return s
}

func init() {
}