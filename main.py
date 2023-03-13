from mmpose.apis import inference_pose_lifter_model, init_pose_model, process_mmdet_results, extract_pose_sequence, \
    vis_3d_pose_result, inference_top_down_pose_model, get_track_id
from mmdet.apis import inference_detector, init_detector
from mmpose.core import Smoother
from mmpose.datasets import DatasetInfo
import mmcv
import copy

""" Based on mmpose/demo/body3d_two_stage_video_demo.py """


def main():
    # cap = cv2.VideoCapture('SwordOne.mov')
    vid = mmcv.VideoReader('SwordOne.mov')
    cfg = mmcv.Config.fromfile(
        'mmpose/configs/body/3d_kpt_sview_rgb_vid/video_pose_lift/h36m/videopose3d_h36m_27frames_fullconv_supervised.py')
    pretrained_model = 'model/videopose_h36m_27frames_fullconv_supervised-fe8fbba9_20210527.pth'
    det_config = 'mmpose/demo/mmdetection_cfg/faster_rcnn_r50_fpn_coco.py'
    det_checkpoint = 'model/faster_rcnn_r50_fpn_1x_coco_20200130-047c8118.pth'

    cfg.data_cfg.image_size = (256, 256)
    pose_model = init_pose_model(cfg, pretrained_model)
    det_model = init_detector(det_config, det_checkpoint)

    dataset_info = DatasetInfo(pose_model.cfg.data['test'].get('dataset_info', None))

    filter_cfg = dict(type='GaussianFilter', window_size=3)
    smoother = Smoother(filter_cfg)
    next_id = 0
    pose_det_results_list = []
    pose_det_results = []

    print("Stage 1: 2D poses")
    for cur_frame in mmcv.track_iter_progress(vid):
        pose_det_results_last = pose_det_results

        mmdet_results = inference_detector(det_model, cur_frame)
        person_results = process_mmdet_results(mmdet_results, cat_id=1)

        pose_det_results, _ = inference_top_down_pose_model(
            pose_model,
            cur_frame,
            person_results,
            format='xyxy',
            dataset_info=dataset_info)

        pose_det_results, next_id = get_track_id(
            pose_det_results,
            pose_det_results_last,
            next_id,
            use_oks=False)

        pose_det_results_list.append(copy.deepcopy(pose_det_results))
        # TODO: FIX ASSERTATION ERROR


if __name__ == '__main__':
    main()
