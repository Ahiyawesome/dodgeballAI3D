from mmpose.apis import inference_pose_lifter_model, init_pose_model, process_mmdet_results, extract_pose_sequence, \
    vis_3d_pose_result, inference_top_down_pose_model, get_track_id
from mmdet.apis import inference_detector, init_detector
from mmpose.core import Smoother
from mmpose.datasets import DatasetInfo
from demo.body3d_two_stage_video_demo import convert_keypoint_definition
import os.path as osp
import mmcv
import cv2
import copy
import pickle

""" Based on mmpose/demo/body3d_two_stage_video_demo.py """


def main():
    vid = mmcv.VideoReader('SwordFight.mp4')
    cfg = mmcv.Config.fromfile('mmpose/configs/body/2d_kpt_sview_rgb_img/topdown_heatmap/coco/mspn50_coco_256x192.py')
    pretrained_model = 'model/mspn50_coco_256x192-8fbfb5d0_20201123.pth'
    det_config = 'mmpose/demo/mmdetection_cfg/faster_rcnn_r50_fpn_coco.py'
    det_checkpoint = 'model/faster_rcnn_r50_fpn_1x_coco_20200130-047c8118.pth'
    pose_lift_config = mmcv.Config.fromfile('mmpose/configs/body/3d_kpt_sview_rgb_vid/video_pose_lift/h36m/videopose3d_h36m_243frames_fullconv_supervised_cpn_ft.py')
    pose_lift_checkpoint = 'model/videopose_h36m_243frames_fullconv_supervised_cpn_ft-88f5abbb_20210527.pth'

    # cfg.data_cfg.image_size = (256, 256)
    pose_model = init_pose_model(cfg, pretrained_model)
    pose_det_dataset = pose_model.cfg.data['test']['type']
    det_model = init_detector(det_config, det_checkpoint)

    dataset_info = DatasetInfo(pose_model.cfg.data['test'].get('dataset_info', None))

    filter_cfg = dict(type='GaussianFilter', window_size=3)
    smoother = Smoother(filter_cfg)
    next_id = 0
    need_extraction = False
    pose_det_results_list = []
    pose_det_results = []

    print("Stage 1: 2D poses")

    # Saving data to pickle file since it takes a long time to extract the vid data
    with open("pose_det_results_list.pickle", "rb") as f:
        try:
            pose_det_results_list = pickle.load(f)
            new_extraction = input("pose detection results list detected, do you want a new one? [y/n]")
            if new_extraction == 'y':
                need_extraction = True
                pose_det_results_list = []
            else:
                print("Continuing onto next stage with loaded data")

        except EOFError:
            need_extraction = True

    if need_extraction:
        for cur_frame in mmcv.track_iter_progress(vid):
            pose_det_results_last = pose_det_results

            mmdet_results = inference_detector(det_model, cur_frame)
            person_results = process_mmdet_results(mmdet_results, cat_id=1)

            pose_det_results, _ = inference_top_down_pose_model(
                pose_model,
                cur_frame,
                person_results,
                format='xyxy',
                dataset=pose_det_dataset,
                dataset_info=dataset_info,
                return_heatmap=False,
                outputs=None)

            pose_det_results, next_id = get_track_id(
                pose_det_results,
                pose_det_results_last,
                next_id,
                use_oks=False)

            pose_det_results_list.append(copy.deepcopy(pose_det_results))

        with open("pose_det_results_list.pickle", "wb") as f:
            pickle.dump(pose_det_results_list, f)

    print('Stage 2: 2D-to-3D pose lifting.')
    # TODO: Do second stage
    pose_lift_model = init_pose_model(pose_lift_config, pose_lift_checkpoint)
    pose_lift_dataset = pose_lift_model.cfg.data['test']['type']

    fourcc = cv2.VideoWriter_fourcc(*'mp4v')
    fps = vid.fps
    writer = None

    for pose_det_results in pose_det_results_list:
        for res in pose_det_results:
            keypoints = res['keypoints']
            res['keypoints'] = convert_keypoint_definition(
                keypoints, pose_det_dataset, pose_lift_dataset)

    if hasattr(pose_lift_model.cfg, 'test_data_cfg'):
        data_cfg = pose_lift_model.cfg.test_data_cfg
    else:
        data_cfg = pose_lift_model.cfg.data_cfg

    pose_lift_dataset_info = pose_lift_model.cfg.data['test'].get(
        'dataset_info', None)

    pose_lift_dataset_info = DatasetInfo(pose_lift_dataset_info)

    num_instances = -1
    print('Running 2D-to-3D pose lifting inference...')
    for i, pose_det_results in enumerate(
            mmcv.track_iter_progress(pose_det_results_list)):
        # extract and pad input pose2d sequence
        pose_results_2d = extract_pose_sequence(
            pose_det_results_list,
            frame_idx=i,
            causal=data_cfg.causal,
            seq_len=data_cfg.seq_len,
            step=data_cfg.seq_frame_interval)

        # 2D-to-3D pose lifting
        pose_lift_results = inference_pose_lifter_model(
            pose_lift_model,
            pose_results_2d=pose_results_2d,
            dataset=pose_lift_dataset,
            dataset_info=pose_lift_dataset_info,
            with_track_id=True,
            image_size=vid.resolution)

        # Pose processing
        pose_lift_results_vis = []
        for idx, res in enumerate(pose_lift_results):
            keypoints_3d = res['keypoints_3d']
            # exchange y,z-axis, and then reverse the direction of x,z-axis
            keypoints_3d = keypoints_3d[..., [0, 2, 1]]
            keypoints_3d[..., 0] = -keypoints_3d[..., 0]
            keypoints_3d[..., 2] = -keypoints_3d[..., 2]
            # rebase height (z-axis)
            res['keypoints_3d'] = keypoints_3d
            # add title
            det_res = pose_det_results[idx]
            instance_id = det_res['track_id']
            res['title'] = f'Prediction ({instance_id})'
            # only visualize the target frame
            res['keypoints'] = det_res['keypoints']
            res['bbox'] = det_res['bbox']
            res['track_id'] = instance_id
            pose_lift_results_vis.append(res)

        # Visualization
        if num_instances < 0:
            num_instances = len(pose_lift_results_vis)
        img_vis = vis_3d_pose_result(
            pose_lift_model,
            result=pose_lift_results_vis,
            img=vid[i],
            dataset=pose_lift_dataset,
            dataset_info=pose_lift_dataset_info,
            out_file=None)

        if writer is None:
            writer = cv2.VideoWriter(
                "SwordFight3.mp4", fourcc,
                fps, (img_vis.shape[1], img_vis.shape[0]))
        writer.write(img_vis)

    writer.release()
    # TODO: Add Pickle for 3D poses, add video file to show all poses.


if __name__ == '__main__':
    main()
