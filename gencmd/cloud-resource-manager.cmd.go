// Code generated by protoc-gen-gogo. DO NOT EDIT.
// source: cloud-resource-manager.proto

package gencmd

import edgeproto "github.com/mobiledgex/edge-cloud/edgeproto"
import "strings"
import "time"
import "strconv"
import "github.com/spf13/cobra"
import "context"
import "os"
import "io"
import "text/tabwriter"
import "github.com/spf13/pflag"
import "errors"
import proto "github.com/gogo/protobuf/proto"
import fmt "fmt"
import math "math"

// Reference imports to suppress errors if they are not otherwise used.
var _ = proto.Marshal
var _ = fmt.Errorf
var _ = math.Inf

// Auto-generated code: DO NOT EDIT
var CloudResourceManagerCmd edgeproto.CloudResourceManagerClient
var CloudResourceIn edgeproto.CloudResource
var CloudResourceFlagSet = pflag.NewFlagSet("CloudResource", pflag.ExitOnError)
var CloudResourceInCategory string
var EdgeCloudApplicationIn edgeproto.EdgeCloudApplication
var EdgeCloudApplicationFlagSet = pflag.NewFlagSet("EdgeCloudApplication", pflag.ExitOnError)

func CloudResourceSlicer(in *edgeproto.CloudResource) []string {
	s := make([]string, 0, 6)
	s = append(s, in.Name)
	s = append(s, edgeproto.CloudResourceCategory_name[int32(in.Category)])
	if in.CloudletKey == nil {
		in.CloudletKey = &edgeproto.CloudletKey{}
	}
	s = append(s, in.CloudletKey.OperatorKey.Name)
	s = append(s, in.CloudletKey.Name)
	s = append(s, strconv.FormatBool(in.Active))
	s = append(s, string(in.Id))
	return s
}

func CloudResourceHeaderSlicer() []string {
	s := make([]string, 0, 6)
	s = append(s, "Name")
	s = append(s, "Category")
	s = append(s, "CloudletKey-OperatorKey-Name")
	s = append(s, "CloudletKey-Name")
	s = append(s, "Active")
	s = append(s, "Id")
	return s
}

func EdgeCloudAppSlicer(in *edgeproto.EdgeCloudApp) []string {
	s := make([]string, 0, 11)
	s = append(s, in.Name)
	s = append(s, in.Repository)
	s = append(s, in.Image)
	s = append(s, in.Cpu)
	s = append(s, in.Memory)
	s = append(s, string(in.Limitfactor))
	s = append(s, in.Exposure)
	s = append(s, string(in.Replicas))
	s = append(s, in.Context)
	s = append(s, in.Namespace)
	if in.AppInstKey == nil {
		in.AppInstKey = &edgeproto.AppInstKey{}
	}
	s = append(s, in.AppInstKey.AppKey.DeveloperKey.Name)
	s = append(s, in.AppInstKey.AppKey.Name)
	s = append(s, in.AppInstKey.AppKey.Version)
	s = append(s, in.AppInstKey.CloudletKey.OperatorKey.Name)
	s = append(s, in.AppInstKey.CloudletKey.Name)
	s = append(s, string(in.AppInstKey.Id))
	return s
}

func EdgeCloudAppHeaderSlicer() []string {
	s := make([]string, 0, 11)
	s = append(s, "Name")
	s = append(s, "Repository")
	s = append(s, "Image")
	s = append(s, "Cpu")
	s = append(s, "Memory")
	s = append(s, "Limitfactor")
	s = append(s, "Exposure")
	s = append(s, "Replicas")
	s = append(s, "Context")
	s = append(s, "Namespace")
	s = append(s, "AppInstKey-AppKey-DeveloperKey-Name")
	s = append(s, "AppInstKey-AppKey-Name")
	s = append(s, "AppInstKey-AppKey-Version")
	s = append(s, "AppInstKey-CloudletKey-OperatorKey-Name")
	s = append(s, "AppInstKey-CloudletKey-Name")
	s = append(s, "AppInstKey-Id")
	return s
}

func EdgeCloudApplicationSlicer(in *edgeproto.EdgeCloudApplication) []string {
	s := make([]string, 0, 3)
	s = append(s, in.Manifest)
	s = append(s, in.Kind)
	if in.Apps == nil {
		in.Apps = make([]*edgeproto.EdgeCloudApp, 1)
		in.Apps[0] = &edgeproto.EdgeCloudApp{}
	}
	s = append(s, in.Apps[0].Name)
	s = append(s, in.Apps[0].Repository)
	s = append(s, in.Apps[0].Image)
	s = append(s, in.Apps[0].Cpu)
	s = append(s, in.Apps[0].Memory)
	s = append(s, string(in.Apps[0].Limitfactor))
	s = append(s, in.Apps[0].Exposure)
	s = append(s, string(in.Apps[0].Replicas))
	s = append(s, in.Apps[0].Context)
	s = append(s, in.Apps[0].Namespace)
	if in.Apps[0].AppInstKey == nil {
		in.Apps[0].AppInstKey = &edgeproto.AppInstKey{}
	}
	s = append(s, in.Apps[0].AppInstKey.AppKey.DeveloperKey.Name)
	s = append(s, in.Apps[0].AppInstKey.AppKey.Name)
	s = append(s, in.Apps[0].AppInstKey.AppKey.Version)
	s = append(s, in.Apps[0].AppInstKey.CloudletKey.OperatorKey.Name)
	s = append(s, in.Apps[0].AppInstKey.CloudletKey.Name)
	s = append(s, string(in.Apps[0].AppInstKey.Id))
	return s
}

func EdgeCloudApplicationHeaderSlicer() []string {
	s := make([]string, 0, 3)
	s = append(s, "Manifest")
	s = append(s, "Kind")
	s = append(s, "Apps-Name")
	s = append(s, "Apps-Repository")
	s = append(s, "Apps-Image")
	s = append(s, "Apps-Cpu")
	s = append(s, "Apps-Memory")
	s = append(s, "Apps-Limitfactor")
	s = append(s, "Apps-Exposure")
	s = append(s, "Apps-Replicas")
	s = append(s, "Apps-Context")
	s = append(s, "Apps-Namespace")
	s = append(s, "Apps-AppInstKey-AppKey-DeveloperKey-Name")
	s = append(s, "Apps-AppInstKey-AppKey-Name")
	s = append(s, "Apps-AppInstKey-AppKey-Version")
	s = append(s, "Apps-AppInstKey-CloudletKey-OperatorKey-Name")
	s = append(s, "Apps-AppInstKey-CloudletKey-Name")
	s = append(s, "Apps-AppInstKey-Id")
	return s
}

var ListCloudResourceCmd = &cobra.Command{
	Use: "ListCloudResource",
	Run: func(cmd *cobra.Command, args []string) {
		if CloudResourceManagerCmd == nil {
			fmt.Println("CloudResourceManager client not initialized")
			return
		}
		var err error
		err = parseCloudResourceEnums()
		if err != nil {
			fmt.Println("ListCloudResource: ", err)
			return
		}
		ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
		output := tabwriter.NewWriter(os.Stdout, 0, 0, 1, ' ', 0)
		count := 0
		fmt.Fprintln(output, strings.Join(CloudResourceHeaderSlicer(), "\t"))
		defer cancel()
		stream, err := CloudResourceManagerCmd.ListCloudResource(ctx, &CloudResourceIn)
		if err != nil {
			fmt.Println("ListCloudResource failed: ", err)
			return
		}
		for {
			obj, err := stream.Recv()
			if err == io.EOF {
				break
			}
			if err != nil {
				fmt.Println("ListCloudResource recv failed: ", err)
				break
			}
			fmt.Fprintln(output, strings.Join(CloudResourceSlicer(obj), "\t"))
			count++
		}
		if count > 0 {
			output.Flush()
		}
	},
}

var AddCloudResourceCmd = &cobra.Command{
	Use: "AddCloudResource",
	Run: func(cmd *cobra.Command, args []string) {
		if CloudResourceManagerCmd == nil {
			fmt.Println("CloudResourceManager client not initialized")
			return
		}
		var err error
		err = parseCloudResourceEnums()
		if err != nil {
			fmt.Println("AddCloudResource: ", err)
			return
		}
		ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
		out, err := CloudResourceManagerCmd.AddCloudResource(ctx, &CloudResourceIn)
		cancel()
		if err != nil {
			fmt.Println("AddCloudResource failed: ", err)
		} else {
			headers := ResultHeaderSlicer()
			data := ResultSlicer(out)
			for ii := 0; ii < len(headers) && ii < len(data); ii++ {
				fmt.Println(headers[ii] + ": " + data[ii])
			}
		}
	},
}

var DeleteCloudResourceCmd = &cobra.Command{
	Use: "DeleteCloudResource",
	Run: func(cmd *cobra.Command, args []string) {
		if CloudResourceManagerCmd == nil {
			fmt.Println("CloudResourceManager client not initialized")
			return
		}
		var err error
		err = parseCloudResourceEnums()
		if err != nil {
			fmt.Println("DeleteCloudResource: ", err)
			return
		}
		ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
		out, err := CloudResourceManagerCmd.DeleteCloudResource(ctx, &CloudResourceIn)
		cancel()
		if err != nil {
			fmt.Println("DeleteCloudResource failed: ", err)
		} else {
			headers := ResultHeaderSlicer()
			data := ResultSlicer(out)
			for ii := 0; ii < len(headers) && ii < len(data); ii++ {
				fmt.Println(headers[ii] + ": " + data[ii])
			}
		}
	},
}

var DeployApplicationCmd = &cobra.Command{
	Use: "DeployApplication",
	Run: func(cmd *cobra.Command, args []string) {
		if CloudResourceManagerCmd == nil {
			fmt.Println("CloudResourceManager client not initialized")
			return
		}
		var err error
		ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
		out, err := CloudResourceManagerCmd.DeployApplication(ctx, &EdgeCloudApplicationIn)
		cancel()
		if err != nil {
			fmt.Println("DeployApplication failed: ", err)
		} else {
			headers := ResultHeaderSlicer()
			data := ResultSlicer(out)
			for ii := 0; ii < len(headers) && ii < len(data); ii++ {
				fmt.Println(headers[ii] + ": " + data[ii])
			}
		}
	},
}

var DeleteApplicationCmd = &cobra.Command{
	Use: "DeleteApplication",
	Run: func(cmd *cobra.Command, args []string) {
		if CloudResourceManagerCmd == nil {
			fmt.Println("CloudResourceManager client not initialized")
			return
		}
		var err error
		ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
		out, err := CloudResourceManagerCmd.DeleteApplication(ctx, &EdgeCloudApplicationIn)
		cancel()
		if err != nil {
			fmt.Println("DeleteApplication failed: ", err)
		} else {
			headers := ResultHeaderSlicer()
			data := ResultSlicer(out)
			for ii := 0; ii < len(headers) && ii < len(data); ii++ {
				fmt.Println(headers[ii] + ": " + data[ii])
			}
		}
	},
}

func init() {
	CloudResourceFlagSet.StringVar(&CloudResourceIn.Name, "name", "", "Name")
	CloudResourceFlagSet.StringVar(&CloudResourceInCategory, "category", "", "CloudResourceInCategory")
	CloudResourceIn.CloudletKey = &edgeproto.CloudletKey{}
	CloudResourceFlagSet.StringVar(&CloudResourceIn.CloudletKey.OperatorKey.Name, "cloudletkey-operatorkey-name", "", "CloudletKey.OperatorKey.Name")
	CloudResourceFlagSet.StringVar(&CloudResourceIn.CloudletKey.Name, "cloudletkey-name", "", "CloudletKey.Name")
	CloudResourceFlagSet.BoolVar(&CloudResourceIn.Active, "active", false, "Active")
	CloudResourceFlagSet.Int32Var(&CloudResourceIn.Id, "id", 0, "Id")
	CloudResourceFlagSet.BytesHexVar(&CloudResourceIn.AccessIp, "accessip", nil, "AccessIp")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Manifest, "manifest", "", "Manifest")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Kind, "kind", "", "Kind")
	EdgeCloudApplicationIn.Apps = make([]*edgeproto.EdgeCloudApp, 1)
	EdgeCloudApplicationIn.Apps[0] = &edgeproto.EdgeCloudApp{}
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].Name, "apps-name", "", "Apps[0].Name")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].Repository, "apps-repository", "", "Apps[0].Repository")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].Image, "apps-image", "", "Apps[0].Image")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].Cpu, "apps-cpu", "", "Apps[0].Cpu")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].Memory, "apps-memory", "", "Apps[0].Memory")
	EdgeCloudApplicationFlagSet.Int32Var(&EdgeCloudApplicationIn.Apps[0].Limitfactor, "apps-limitfactor", 0, "Apps[0].Limitfactor")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].Exposure, "apps-exposure", "", "Apps[0].Exposure")
	EdgeCloudApplicationFlagSet.Int32Var(&EdgeCloudApplicationIn.Apps[0].Replicas, "apps-replicas", 0, "Apps[0].Replicas")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].Context, "apps-context", "", "Apps[0].Context")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].Namespace, "apps-namespace", "", "Apps[0].Namespace")
	EdgeCloudApplicationIn.Apps[0].AppInstKey = &edgeproto.AppInstKey{}
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].AppInstKey.AppKey.DeveloperKey.Name, "apps-appinstkey-appkey-developerkey-name", "", "Apps[0].AppInstKey.AppKey.DeveloperKey.Name")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].AppInstKey.AppKey.Name, "apps-appinstkey-appkey-name", "", "Apps[0].AppInstKey.AppKey.Name")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].AppInstKey.AppKey.Version, "apps-appinstkey-appkey-version", "", "Apps[0].AppInstKey.AppKey.Version")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].AppInstKey.CloudletKey.OperatorKey.Name, "apps-appinstkey-cloudletkey-operatorkey-name", "", "Apps[0].AppInstKey.CloudletKey.OperatorKey.Name")
	EdgeCloudApplicationFlagSet.StringVar(&EdgeCloudApplicationIn.Apps[0].AppInstKey.CloudletKey.Name, "apps-appinstkey-cloudletkey-name", "", "Apps[0].AppInstKey.CloudletKey.Name")
	EdgeCloudApplicationFlagSet.Uint64Var(&EdgeCloudApplicationIn.Apps[0].AppInstKey.Id, "apps-appinstkey-id", 0, "Apps[0].AppInstKey.Id")
	ListCloudResourceCmd.Flags().AddFlagSet(CloudResourceFlagSet)
	AddCloudResourceCmd.Flags().AddFlagSet(CloudResourceFlagSet)
	DeleteCloudResourceCmd.Flags().AddFlagSet(CloudResourceFlagSet)
	DeployApplicationCmd.Flags().AddFlagSet(EdgeCloudApplicationFlagSet)
	DeleteApplicationCmd.Flags().AddFlagSet(EdgeCloudApplicationFlagSet)
}

func parseCloudResourceEnums() error {
	if CloudResourceInCategory != "" {
		switch CloudResourceInCategory {
		case "allcloudresources":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(0)
		case "kubernetes":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(200)
		case "k8s":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(200)
		case "mesos":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(201)
		case "aws":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(202)
		case "gcp":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(203)
		case "azure":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(204)
		case "digitalocean":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(205)
		case "packetnet":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(206)
		case "openstack":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(300)
		case "docker":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(301)
		case "eks":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(400)
		case "aks":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(402)
		case "gks":
			CloudResourceIn.Category = edgeproto.CloudResourceCategory(403)
		default:
			return errors.New("Invalid value for CloudResourceInCategory")
		}
	}
	return nil
}