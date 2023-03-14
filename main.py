from mmpose.apis import inference_pose_lifter_model, init_pose_model, process_mmdet_results, extract_pose_sequence, \
    vis_3d_pose_result, inference_top_down_pose_model, get_track_id
from mmdet.apis import inference_detector, init_detector
from mmpose.core import Smoother
from mmpose.datasets import DatasetInfo
import mmcv
import copy
import pickle

""" Based on mmpose/demo/body3d_two_stage_video_demo.py """


def main():
    vid = mmcv.VideoReader('SwordFight.mp4')
    # cfg = mmcv.Config.fromfile(
    #     'mmpose/configs/body/3d_kpt_sview_rgb_vid/video_pose_lift/h36m/videopose3d_h36m_27frames_fullconv_supervised.py')
    # pretrained_model = 'model/videopose_h36m_27frames_fullconv_supervised-fe8fbba9_20210527.pth'
    cfg = mmcv.Config.fromfile('mmpose/configs/body/2d_kpt_sview_rgb_img/topdown_heatmap/coco/mspn50_coco_256x192.py')
    pretrained_model = 'model/mspn50_coco_256x192-8fbfb5d0_20201123.pth'
    det_config = 'mmpose/demo/mmdetection_cfg/faster_rcnn_r50_fpn_coco.py'
    det_checkpoint = 'model/faster_rcnn_r50_fpn_1x_coco_20200130-047c8118.pth'

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


if __name__ == '__main__':
    main()
