// Code generated by protoc-gen-gogo. DO NOT EDIT.
// source: cloudlet.proto

package gencmd

import edgeproto "github.com/mobiledgex/edge-cloud/edgeproto"
import google_protobuf "github.com/gogo/protobuf/types"
import "strings"
import "time"
import "strconv"
import "github.com/spf13/cobra"
import "context"
import "os"
import "io"
import "text/tabwriter"
import "github.com/spf13/pflag"
import proto "github.com/gogo/protobuf/proto"
import fmt "fmt"
import math "math"
import _ "github.com/gogo/googleapis/google/api"
import _ "github.com/mobiledgex/edge-cloud/protogen"
import _ "github.com/gogo/protobuf/gogoproto"

// Reference imports to suppress errors if they are not otherwise used.
var _ = proto.Marshal
var _ = fmt.Errorf
var _ = math.Inf

// Auto-generated code: DO NOT EDIT
var CloudletApiCmd edgeproto.CloudletApiClient
var CloudletIn edgeproto.Cloudlet
var CloudletFlagSet = pflag.NewFlagSet("Cloudlet", pflag.ExitOnError)

func CloudletKeySlicer(in *edgeproto.CloudletKey) []string {
	s := make([]string, 0, 2)
	s = append(s, in.OperatorKey.Name)
	s = append(s, in.Name)
	return s
}

func CloudletKeyHeaderSlicer() []string {
	s := make([]string, 0, 2)
	s = append(s, "OperatorKey-Name")
	s = append(s, "Name")
	return s
}

func CloudletSlicer(in *edgeproto.Cloudlet) []string {
	s := make([]string, 0, 4)
	s = append(s, in.Key.OperatorKey.Name)
	s = append(s, in.Key.Name)
	s = append(s, strconv.FormatFloat(float64(in.Location.Lat), 'e', -1, 32))
	s = append(s, strconv.FormatFloat(float64(in.Location.Long), 'e', -1, 32))
	s = append(s, strconv.FormatFloat(float64(in.Location.HorizontalAccuracy), 'e', -1, 32))
	s = append(s, strconv.FormatFloat(float64(in.Location.VerticalAccuracy), 'e', -1, 32))
	s = append(s, strconv.FormatFloat(float64(in.Location.Altitude), 'e', -1, 32))
	s = append(s, strconv.FormatFloat(float64(in.Location.Course), 'e', -1, 32))
	s = append(s, strconv.FormatFloat(float64(in.Location.Speed), 'e', -1, 32))
	if in.Location.Timestamp == nil {
		in.Location.Timestamp = &google_protobuf.Timestamp{}
	}
	timestampTime := time.Unix(in.Location.Timestamp.Seconds, int64(in.Location.Timestamp.Nanos))
	s = append(s, timestampTime.String())
	return s
}

func CloudletHeaderSlicer() []string {
	s := make([]string, 0, 4)
	s = append(s, "Key-OperatorKey-Name")
	s = append(s, "Key-Name")
	s = append(s, "Location-Lat")
	s = append(s, "Location-Long")
	s = append(s, "Location-HorizontalAccuracy")
	s = append(s, "Location-VerticalAccuracy")
	s = append(s, "Location-Altitude")
	s = append(s, "Location-Course")
	s = append(s, "Location-Speed")
	s = append(s, "Location-Timestamp")
	return s
}

var CreateCloudletCmd = &cobra.Command{
	Use: "CreateCloudlet",
	Run: func(cmd *cobra.Command, args []string) {
		if CloudletApiCmd == nil {
			fmt.Println("CloudletApi client not initialized")
			return
		}
		var err error
		ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
		out, err := CloudletApiCmd.CreateCloudlet(ctx, &CloudletIn)
		cancel()
		if err != nil {
			fmt.Println("CreateCloudlet failed: ", err)
		} else {
			headers := ResultHeaderSlicer()
			data := ResultSlicer(out)
			for ii := 0; ii < len(headers) && ii < len(data); ii++ {
				fmt.Println(headers[ii] + ": " + data[ii])
			}
		}
	},
}

var DeleteCloudletCmd = &cobra.Command{
	Use: "DeleteCloudlet",
	Run: func(cmd *cobra.Command, args []string) {
		if CloudletApiCmd == nil {
			fmt.Println("CloudletApi client not initialized")
			return
		}
		var err error
		ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
		out, err := CloudletApiCmd.DeleteCloudlet(ctx, &CloudletIn)
		cancel()
		if err != nil {
			fmt.Println("DeleteCloudlet failed: ", err)
		} else {
			headers := ResultHeaderSlicer()
			data := ResultSlicer(out)
			for ii := 0; ii < len(headers) && ii < len(data); ii++ {
				fmt.Println(headers[ii] + ": " + data[ii])
			}
		}
	},
}

var UpdateCloudletCmd = &cobra.Command{
	Use: "UpdateCloudlet",
	Run: func(cmd *cobra.Command, args []string) {
		if CloudletApiCmd == nil {
			fmt.Println("CloudletApi client not initialized")
			return
		}
		var err error
		ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
		out, err := CloudletApiCmd.UpdateCloudlet(ctx, &CloudletIn)
		cancel()
		if err != nil {
			fmt.Println("UpdateCloudlet failed: ", err)
		} else {
			headers := ResultHeaderSlicer()
			data := ResultSlicer(out)
			for ii := 0; ii < len(headers) && ii < len(data); ii++ {
				fmt.Println(headers[ii] + ": " + data[ii])
			}
		}
	},
}

var ShowCloudletCmd = &cobra.Command{
	Use: "ShowCloudlet",
	Run: func(cmd *cobra.Command, args []string) {
		if CloudletApiCmd == nil {
			fmt.Println("CloudletApi client not initialized")
			return
		}
		var err error
		ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
		output := tabwriter.NewWriter(os.Stdout, 0, 0, 1, ' ', 0)
		count := 0
		fmt.Fprintln(output, strings.Join(CloudletHeaderSlicer(), "\t"))
		defer cancel()
		stream, err := CloudletApiCmd.ShowCloudlet(ctx, &CloudletIn)
		if err != nil {
			fmt.Println("ShowCloudlet failed: ", err)
			return
		}
		for {
			obj, err := stream.Recv()
			if err == io.EOF {
				break
			}
			if err != nil {
				fmt.Println("ShowCloudlet recv failed: ", err)
				break
			}
			fmt.Fprintln(output, strings.Join(CloudletSlicer(obj), "\t"))
			count++
		}
		if count > 0 {
			output.Flush()
		}
	},
}

func init() {
	CloudletFlagSet.StringVar(&CloudletIn.Key.OperatorKey.Name, "key-operatorkey-name", "", "Key.OperatorKey.Name")
	CloudletFlagSet.StringVar(&CloudletIn.Key.Name, "key-name", "", "Key.Name")
	CloudletFlagSet.BytesHexVar(&CloudletIn.AccessIp, "accessip", nil, "AccessIp")
	CloudletFlagSet.Float64Var(&CloudletIn.Location.Lat, "location-lat", 0, "Location.Lat")
	CloudletFlagSet.Float64Var(&CloudletIn.Location.Long, "location-long", 0, "Location.Long")
	CloudletFlagSet.Float64Var(&CloudletIn.Location.HorizontalAccuracy, "location-horizontalaccuracy", 0, "Location.HorizontalAccuracy")
	CloudletFlagSet.Float64Var(&CloudletIn.Location.VerticalAccuracy, "location-verticalaccuracy", 0, "Location.VerticalAccuracy")
	CloudletFlagSet.Float64Var(&CloudletIn.Location.Altitude, "location-altitude", 0, "Location.Altitude")
	CloudletFlagSet.Float64Var(&CloudletIn.Location.Course, "location-course", 0, "Location.Course")
	CloudletFlagSet.Float64Var(&CloudletIn.Location.Speed, "location-speed", 0, "Location.Speed")
	CloudletIn.Location.Timestamp = &google_protobuf.Timestamp{}
	CloudletFlagSet.Int64Var(&CloudletIn.Location.Timestamp.Seconds, "location-timestamp-seconds", 0, "Location.Timestamp.Seconds")
	CloudletFlagSet.Int32Var(&CloudletIn.Location.Timestamp.Nanos, "location-timestamp-nanos", 0, "Location.Timestamp.Nanos")
	CreateCloudletCmd.Flags().AddFlagSet(CloudletFlagSet)
	DeleteCloudletCmd.Flags().AddFlagSet(CloudletFlagSet)
	UpdateCloudletCmd.Flags().AddFlagSet(CloudletFlagSet)
	ShowCloudletCmd.Flags().AddFlagSet(CloudletFlagSet)
}
